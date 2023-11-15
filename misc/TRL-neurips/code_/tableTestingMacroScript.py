import pandas as pd
from abc import ABC, abstractmethod
import itertools
from typing import Any, Callable, Iterator, List, Optional
from dataclasses import dataclass
import numpy as np
import json
import jsonlines
import os
import datetime
from prompts import *
from LLMCall import openapi_call_completions
from tabulate import tabulate
import random
import string
from collections import Counter
from itertools import combinations
from utils import Convert_back_to_df, num_tokens_from_string, stringify_serialized_df
from tqdm.auto import tqdm


class CustomJSONEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, np.integer):
            return int(obj)
        elif isinstance(obj, np.floating):
            return float(obj)
        elif isinstance(obj, np.ndarray):
            return obj.tolist()
        if isinstance(obj, pd.Series):
            return obj.tolist()
        elif isinstance(obj, datetime):
            # Handle datetime objects
            return obj.isoformat()
        elif isinstance(obj, set):
            # Handle sets
            return list(obj)
        return super().default(obj)


@dataclass
class TestCase:
    question: str
    expect: Any
    TestType: str


RNG_SEED = 42


def get_random_state(random_state=None):
    if random_state is None:
        random_state = np.random.RandomState(seed=RNG_SEED)
    return random_state


def create_combinations(entries: List) -> List:
    result = []
    i = 0
    while i < len(entries):
        if (len(entries) - i)not in [1, 2]:
            if i < int(len(entries)/2):
                lower_bound = 2
            else:
                lower_bound = 1
            tuple_size = min(4, np.random.randint(
                lower_bound, (len(entries) - i)))
        else:
            tuple_size = len(entries) - i
        result.append(tuple(entries[i:i + tuple_size]))
        i += tuple_size
    return result


def combine_columns(df, combinations_list):
    new_df = pd.DataFrame()
    column_names = df.columns.to_list()
    for en in combinations_list:
        column_subset_names = list(np.array(column_names)[list(en)])
        new_df[str("-----".join(column_subset_names))] = df[column_subset_names].apply(
            lambda row: '-----'.join(map(str, row)), axis=1)
    return new_df


class TestCaseGenerator(ABC):

    def generate(
        self,
        df: pd.DataFrame,
        random_state: Optional[np.random.RandomState] = None
    ) -> Iterator[TestCase]:
        raise NotImplementedError()

    def check(self, test: TestCase, answer: pd.DataFrame) -> bool:

        expected_df = Convert_back_to_df("JsonFormat", str(test.expect))
        expected_df = expected_df.astype(str)
        if expected_df.shape == answer.shape:
            return expected_df.equals(answer)
        else:
            return False

    def metric_pass_k(self, test: TestCase, answer: List[pd.DataFrame], k: int) -> float:
        expected_df = Convert_back_to_df("JsonFormat", str(test.expect))
        expected_df = expected_df.astype(str)
        boolean_answers = [expected_df.equals(
            answer[i]) if expected_df.shape == answer[i].shape else False for i in range(len(answer))]
        combinations_k = list(combinations(boolean_answers, k))
        passed_at_k = 0
        # Calculate the pass@k metric
        for comb in combinations_k:
            if any(comb):
                passed_at_k += 1
        pass_at_k_percentage = (passed_at_k / len(combinations_k))*100

        return pass_at_k_percentage

    def per_cell_accuracies(self, test: TestCase, answer: List[pd.DataFrame]) -> List[float]:
        score_list = []
        for ans in answer:
            expected_df = Convert_back_to_df("JsonFormat", str(test.expect))
            expected_df = expected_df.astype(str)
            if expected_df.shape == ans.shape:
                matching_cells = (expected_df == ans).sum().sum()
                total_cells = expected_df.shape[0]*expected_df.shape[1]
                per_cell_score = matching_cells/total_cells
            else:
                per_cell_score = 0.0
            score_list.append(np.round(per_cell_score, 2))
        return score_list


class NavigationTests(TestCaseGenerator):

    def generate(self, df, random_state=None):
        random_state = get_random_state(random_state)
        cols = df.columns
        indexes = df.index.to_list()
        nrows = df.shape[0]
        while True:
            col_idx = random_state.choice(cols)
            row_idx = random_state.randint(0, nrows)
            question = f"What value is at row {indexes[row_idx]} and column {col_idx}?"
            yield TestCase(question, df.iloc[row_idx][col_idx], "NavigationTests")


class ColumnLookupTests(TestCaseGenerator):

    def generate(self, df, random_state=None):

        random_state = get_random_state(random_state)
        cols = df.columns
        nrows = df.shape[0]
        while True:
            col_idx = random_state.choice(cols)
            row_idx = random_state.randint(0, nrows)

            value = df.iloc[row_idx][col_idx]
            question = f"What column is the {value} in?"

            _, col_indices = np.where(df.to_numpy() == value)
            col_indices = list(set([cols[i] for i in col_indices.tolist()]))

            yield TestCase(question, col_indices, "ColumnLookupTests")


class RowLookupTests(TestCaseGenerator):

    def generate(self, df, random_state=None):

        random_state = get_random_state(random_state)
        cols = df.columns
        indexes = df.index.to_list()
        nrows = df.shape[0]
        while True:
            col_idx = random_state.choice(cols)
            row_idx = random_state.randint(0, nrows)

            value = df.iloc[row_idx][col_idx]
            question = f"What row is the {value} in?"

            row_indices, _ = np.where(df.to_numpy() == value)
            row_indices = list(set(row_indices.tolist()))
            row_indices_ = [indexes[v] for v in row_indices]
            yield TestCase(question, row_indices_, "RowLookupTests")


class DataTypeLookupTests(TestCaseGenerator):

    def generate(self, df, random_state=None):
        df = df.copy(deep=True)
        random_state = get_random_state(random_state)

        indices = list(df.index)
        transpose_bool = all(isinstance(item, str) for item in indices)
        header = "column"
        if transpose_bool:
            df = df.T
            header = "row"
        cols = list(df.columns)
        for col in cols*100:
            question = f"What type (using Pandas datatype notation) is {header} {col}?"
            answer = str(df.dtypes[col])
            yield TestCase(question, answer, "DataTypeLookupTests")


class TableReconstructionTests(TestCaseGenerator):
    def generate(self, df, random_state=None):
        question = f"Can you reconstruct the table by deserializing the table above?"

        answer = df.to_json(orient='index', index=True)

        yield TestCase(question, answer, "TableReconstructionTests")


class TableReconstructionTests1(TestCaseGenerator):
    def generate(self, df, random_state=None):
        question = f"Can you reconstruct the table by deserializing the table above?"

        answer = df.to_json(orient='index', index=True)

        yield TestCase(question, answer, "TableReconstructionTests1")


class TableColumnReorderTests(TestCaseGenerator):
    def generate(self, df, random_state=None):
        random_state = get_random_state(random_state)
        cols = list(df.columns)
        while True:
            random_state.shuffle(cols)
            new_df = df[cols]
            new_column_order = new_df.columns.to_list()
            question = f"""Can you reorder the table such that the column are in this new order {str(new_column_order)}? Make sure to return the complete reordered table."""
            answer = new_df.to_json(orient='index', index=True)
            yield TestCase(question, answer, "ColumnShuffleTests")


class TableTransposeTests(TestCaseGenerator):
    def generate(self, df, random_state=None):
        question = f"""Can you transpose the table?"""

        answer = df.T.to_json(orient='index', index=True)
        yield TestCase(question, answer, "ColumnShuffleTests")


class TableOperation(ABC):
    @abstractmethod
    def modify(
        self, df: pd.DataFrame, random_state: Optional[np.random.RandomState]
    ) -> Iterator[pd.DataFrame]:
        raise NotImplementedError()


class ShuffleRows(TableOperation):

    def modify(self, df, random_state=None):
        df = df.copy(deep=True)
        random_state = get_random_state(random_state)
        while True:
            yield df.sample(frac=1.0, random_state=random_state, replace=False)


class ShuffleColumns(TableOperation):

    def modify(self, df, random_state=None):
        df = df.copy(deep=True)
        random_state = get_random_state(random_state)
        cols = list(df.columns)
        while True:
            random_state.shuffle(cols)
            yield df[cols]


class ArbitraryColumnNames(TableOperation):

    def __init__(self, get_column_name: Optional[Callable[[int], str]] = None):
        if get_column_name is None:
            get_column_name = []
            while len(get_column_name) <= 200:
                arb_name = ''.join(random.choices(
                    string.ascii_letters + string.digits, k=np.random.randint(1, 11)))
                if arb_name not in get_column_name:
                    get_column_name.append(arb_name)
        self.get_column_name = get_column_name

    def modify(self, df, random_state=None):
        df = df.copy(deep=True)
        while True:
            new_columns = random.sample(self.get_column_name, len(df.columns))
            df.columns = new_columns
            yield df


class SequentialColumnNames(TableOperation):

    def __init__(self, get_column_name: Optional[Callable[[int], str]] = None):
        if get_column_name is None:
            def get_column_name(col_idx): return f"col_{col_idx}"
        self.get_column_name = get_column_name

    def modify(self, df, random_state=None):
        df = df.copy(deep=True)
        random_state = get_random_state(random_state)
        new_columns = [
            self.get_column_name(idx) for idx in range(len(df.columns))
        ]
        df.columns = new_columns

        while True:
            yield df.sample(frac=0.7,
                            random_state=random_state,
                            replace=False,
                            )


class OriginalData(TableOperation):
    def modify(self, df, random_state=None):
        df = df.copy(deep=True)
        yield df


class ShuffleColumnNames(TableOperation):

    def modify(self, df, random_state=None):
        df = df.copy(deep=True)
        random_state = get_random_state(random_state)
        cols = list(df.columns)
        while True:
            random_state.shuffle(cols)
            df.columns = cols
            yield df


class SampleRows(TableOperation):

    def __init__(self, fraction: float = 0.5, replace: bool = True):
        self.fraction = fraction
        self.replace = replace

    def modify(self, df, random_state=None):
        random_state = get_random_state(random_state)
        while True:
            yield df.sample(frac=self.fraction,
                            random_state=random_state,
                            replace=self.replace,
                            ignore_index=True)


class TransposeTable(TableOperation):
    def modify(self, df, random_state=None):
        df = df.copy(deep=True)
        df = df.T
        random_state = get_random_state(random_state)
        while True:
            yield df.sample(frac=0.7,
                            random_state=random_state,
                            replace=False,
                            )


class ColumnCluster(TableOperation):
    def __init__(self, get_column_combination: Optional[Callable[[int], str]] = None):
        if get_column_combination is None:
            get_column_combination = create_combinations
        self.get_column_combination = get_column_combination

    def modify(self, df, random_state=None):
        df = df.copy(deep=True)
        while True:
            combinations_list = self.get_column_combination(range(df.shape[1]))

            new_df = pd.DataFrame()
            column_names = df.columns.to_list()
            for en in combinations_list:
                column_subset_names = list(np.array(column_names)[list(en)])
                new_df[str("-----".join(column_subset_names))] = df[column_subset_names].apply(
                    lambda row: '-----'.join(map(str, row)), axis=1)

            yield new_df


class SerializeTable(TableOperation):
    def modify(self, df, random_state=None):
        df = df.copy(deep=True)
        new_df = pd.DataFrame()
        new_df[""] = df.apply(lambda row: ','.join(
            [f'{col}:{value}' for col, value in row.items()]), axis=1)

        random_state = get_random_state(random_state)
        while True:
            yield new_df.sample(frac=0.7,
                                random_state=random_state,
                                replace=False,
                                )


class CompositeTableOperation(object):

    def __init__(self, table_ops: List[TableOperation]):
        self.table_ops = table_ops

    def modify(self, df, random_state=None):
        random_state = get_random_state(random_state)

        def recursive_app(val, table_ops, random_state):
            if len(table_ops) == 0:
                yield val
            else:
                table_op = table_ops[0]
                for new_val in table_op.modify(val, random_state=random_state):
                    for recursed_val in recursive_app(new_val, table_ops[1:],
                                                      random_state):
                        yield recursed_val

        for df_variant in recursive_app(df, self.table_ops, random_state):
            yield df_variant


class TableFormats(ABC):
    @abstractmethod
    def formatting(
            self, df: pd.DataFrame) -> pd.DataFrame:
        raise NotImplementedError()


class MarkdownFormat(TableFormats):
    def formatting(self, df):
        return tabulate(df, headers='keys', tablefmt='pipe', showindex=True)


class DataMatrixFormat(TableFormats):
    def formatting(self, df):
        df_ = pd.DataFrame()
        df_[""] = df.index.to_list()
        df_[df.columns] = df.values.tolist()
        matrix = df_.columns.tolist()  # Get column headers as a list
        matrix_data = df_.values.tolist()  # Get data as a list of lists
        matrix_data.insert(0, matrix)
        return matrix_data


class JsonFormat(TableFormats):
    def formatting(self, df):

        return df.to_json(orient='index', index=True)


class DFloaderFormat(TableFormats):
    def formatting(self, df):

        str_df_loader = "pd.DataFrame({[data]}, index=[indices])"
        data = ""
        indi = str(df.index.to_list())
        for d in df.columns:
            data += f"'{str(d)}'" + " : " + str(df[d].to_list()) + ", "
        data = data[:-2]
        str_df_loader = str_df_loader.replace(
            "[data]", data).replace("[indices]", indi)
        return str_df_loader


class HTMLFormat(TableFormats):
    def formatting(self, df):
        html = df.to_html(index=True)
        return html


class HTMLNoSpaceFormat(TableFormats):
    def formatting(self, df):
        html = df.to_html(index=True)
        return str(html).replace("\t", "").replace("\n", "").replace("   ", "")


class TabSeparatedFormat(TableFormats):
    def formatting(self, df: pd.DataFrame) -> str:
        return df.to_csv(sep='\t', index=True)


class CommaSeparatedFormat(TableFormats):
    def formatting(self, df: pd.DataFrame) -> str:
        return df.to_csv(index=True)


class SQLQueryFormat(TableFormats):
    def formatting(self, df):
        return


def gather_Examples_Prompt(tab_format, test_case):

    TF, TC = tab_format.__class__.__name__, test_case.__class__.__name__
    if TC in ["TableColumnReorderTests", "TableReconstructionTests", "TableTransposeTests", "TableReconstructionTests1"]:
        Examples = EXAMPLE_Dictionary[f"Ex_{TC}"](
            tab_format, TransposeTable, SerializeTable, ColumnCluster)

    else:
        Examples = EXAMPLES.replace("[Data_format_example1]", EXAMPLE_Dictionary[f"TF_EX1_{TF}"])\
            .replace("[QA1]", EXAMPLE_Dictionary[f"Ex1_QA1_{TC}"])\
            .replace("[Data_format_example2]", EXAMPLE_Dictionary[f"TF_EX1_{TF}"])\
            .replace("[QA2]", EXAMPLE_Dictionary[f"Ex1_QA2_{TC}"])
    return Examples


class LLMTableLearner():
    def get_prompt(self, examples, df_format, question: str, temperature: float) -> Any:
        prompt = DATA_QUES_INSTRUCTION.replace("[Ques]", str(question)).replace(
            "[Data_format]", str(df_format)).replace("[Example]", examples)
        total_tokens = num_tokens_from_string(prompt)
        no_of_token_left = 4051-total_tokens
        expected_token = num_tokens_from_string(str(df_format))+51
        maxTok = min(total_tokens, no_of_token_left)
        num_n = 3
        modelName = "text-davinci-003"
        prompt_cache = {"model": modelName,
                        "prompt": prompt,
                        "temperature": temperature,
                        "max_tokens": maxTok,
                        "top_p": 1,
                        "frequency_penalty": 0,
                        "presence_penalty": 0,
                        "n": num_n,
                        "logprobs": 1}
        return prompt_cache

    def get_answer(self, examples, df_format, question: str, temperature: float, open_api_key: str) -> Any:
        prompt = DATA_QUES_INSTRUCTION.replace("[Ques]", str(question)).replace(
            "[Data_format]", str(df_format)).replace("[Example]", examples)
        total_tokens = num_tokens_from_string(prompt)
        no_of_token_left = 4051-total_tokens
        expected_token = num_tokens_from_string(str(df_format))+51
        maxTok = min(total_tokens, no_of_token_left)
        num_n = 3
        modelName = "text-davinci-003"
        answer, cache = openapi_call_completions(
            prompt, modelName=modelName, temp=temperature, maxTok=maxTok, num_n=num_n, open_api_key=open_api_key)
        return answer, cache


class TableExperimentSuite(object):

    def __init__(self, llm_learner: LLMTableLearner(),
                 table_formats: List[TableFormats],
                 table_ops: List[TableOperation],
                 test_gens: List[TestCaseGenerator],
                 cache_save_path: str,
                 open_api_key: str):
        self.llm = llm_learner
        self.table_formats = table_formats
        self.table_ops = table_ops
        self.test_gens = test_gens
        self.cache_save_path = cache_save_path
        self.open_api_key = open_api_key

    def run_experiment(self,
                       df,
                       per_table_op: int = 10,
                       per_test_gen: int = 10,
                       save_cache=True):
        results = []
        cacahe_all_dict = []
        print("OPEN AI KEY USED: ", self.open_api_key)
        if save_cache:
            current_datetime = datetime.datetime.now()
            formatted_datetime = current_datetime.strftime("%Y_%m_%d_%H_%M_%S")
            cache_path = os.path.join(self.cache_save_path, f"cache_logger")
            if not os.path.exists(cache_path):
                os.makedirs(cache_path)
            cache_path2 = os.path.join(
                self.cache_save_path, f"cache_logger_all")
            if not os.path.exists(cache_path2):
                os.makedirs(cache_path2)
            save_catch_file = os.path.join(
                cache_path, f"macro_test_log_{formatted_datetime}.json")
            save_catch_file2 = os.path.join(
                cache_path, f"macro_test_log_{formatted_datetime}.json")
            with open(save_catch_file, mode="w") as writer:
                pass
        table_ops = [OriginalData(), SampleRows(),
                     ColumnCluster(), SerializeTable()]
        table_ops_test = {"ArbitraryColumnNames": {"TableColumnReorderTests": [5, 10],
                                                   "TableTransposeTests": [50, 1],
                                                   "TableReconstructionTests1": [50, 1]},
                          "ShuffleRows": {"TableColumnReorderTests": [5, 10],
                                          "TableTransposeTests": [50, 1],
                                          "TableReconstructionTests1": [50, 1]},
                          "ColumnCluster": {"TableColumnReorderTests": [5, 10],
                                            "TableTransposeTests": [50, 1],
                                            "TableReconstructionTests1": [50, 1]},
                          "ShuffleColumns": {"TableColumnReorderTests": [5, 10],
                                             "TableTransposeTests": [50, 1],
                                             "TableReconstructionTests1": [50, 1]},
                          "ShuffleColumnNames": {"TableColumnReorderTests": [5, 10],
                                                 "TableTransposeTests": [50, 1],
                                                 "TableReconstructionTests1": [50, 1]},
                          "TransposeTable": {"TableColumnReorderTests": [5, 10],
                                             "TableTransposeTests": [50, 1],
                                             "TableReconstructionTests1": [50, 1]},
                          "SequentialColumnNames": {"TableColumnReorderTests": [5, 10],
                                                    "TableTransposeTests": [50, 1],
                                                    "TableReconstructionTests1": [50, 1]}}

        table_ops_test = {"OriginalData": ["TableColumnReorderTests"],
                          "SampleRows": ["TableTransposeTests"],
                          "ColumnCluster": ["TableReconstructionTests"],
                          "SerializeTable": ["TableReconstructionTests", "TableReconstructionTests1"]}

        for table_op in tqdm(self.table_ops):
            table_op_name = table_op.__class__.__name__
            count_df_variant = 0
            for df_variant in tqdm(itertools.islice(table_op.modify(df),
                                                    per_table_op)):
                df_feed_test = df_variant
                count_df_variant += 1

                for gen in tqdm(self.test_gens):

                    if table_op_name in ["ColumnCluster", "SerializeTable"] and gen.__class__.__name__ == "TableReconstructionTests1":
                        df_feed_test = df
                    for test in itertools.islice(gen.generate(df_feed_test),
                                                 per_test_gen):

                        for tab_format in tqdm(self.table_formats):
                            temperature_list = [0] 
                            for temp in temperature_list:

                                try:
                                    examples = gather_Examples_Prompt(
                                        tab_format, gen)

                                    df_in_desired_format = tab_format.formatting(
                                        df_variant)
                                    prompt_cache_per_call = self.llm.get_prompt(
                                        examples, df_in_desired_format, test.question, temp)

                                    if gen.__class__.__name__ == "TableReconstructionTests1":
                                        df_in_desired_format = stringify_serialized_df(
                                            df_variant)
                                    answer, cache_per_call = self.llm.get_answer(
                                        examples, df_in_desired_format, test.question, temp, self.open_api_key)
                                    error = None
                                    try:
                                        answer_changed_format = [Convert_back_to_df(
                                            tab_format.__class__.__name__, a) for a in answer]
                                        result = gen.check(
                                            test, answer_changed_format[0])
                                        pass_1 = gen.metric_pass_k(
                                            test, answer_changed_format, 1)
                                        pass_3 = gen.metric_pass_k(
                                            test, answer_changed_format, 3)
                                        pass_5 = gen.metric_pass_k(
                                            test, answer_changed_format, 5)
                                        pass_10 = gen.metric_pass_k(
                                            test, answer, 10)
                                        pass_15 = gen.metric_pass_k(
                                            test, answer, 15)
                                        per_cell_accracies = gen.per_cell_accuracies(
                                            test, answer_changed_format)
                                        per_cell_accracies_top1 = per_cell_accracies[0]
                                        error = None
                                    except Exception as Err:
                                        result = None
                                        pass_1 = pass_3 = pass_5 = pass_10 = pass_15 = None
                                        error = str(Err)
                                        per_cell_accracies = []
                                        per_cell_accracies_top1 = None
                                        print(f"error {Err} encountered")

                                except Exception as Err:
                                    answer, cache_per_call = None, None
                                    error = str(Err)
                                    print(f"error {Err} encountered")

                                cache_all = {"model-Temperature": str(temp),
                                             "tab_format": str(tab_format.__class__.__name__),
                                             "table_op": str(table_op_name),
                                             "gen": str(gen.__class__.__name__),
                                             "test-Question": test.question,
                                             "test-expected-answer": test.expect,
                                             "test-TestType": str(test.TestType),
                                             "test": str(test),
                                             "answer": answer,
                                             "result_match_top1": result,
                                             "error": str(error),
                                             "pass_1": str(pass_1),
                                             "pass_3": str(pass_3),
                                             "pass_5": str(pass_5),
                                             "pass_10": str(pass_10),
                                             "pass_15": str(pass_15),
                                             "per_cell_accracies": str(per_cell_accracies),
                                             "prompt_cache_per_call": prompt_cache_per_call,
                                             "LLMOutput": cache_per_call}
                                results.append((temp, tab_format.__class__.__name__, table_op.__class__.__name__, gen.__class__.__name__, test.question, test.expect,
                                               test.TestType, pass_1, pass_3, pass_5, pass_10, pass_15, test, answer, answer, per_cell_accracies_top1, per_cell_accracies, result, error))
                                # cacahe_all_dict.append(cache_all)
                            if save_cache:

                                with open(save_catch_file, mode="a") as file:
                                    serializable_entry = json.dumps(
                                        cache_all, cls=CustomJSONEncoder)
                                    # Add a newline separator
                                    file.write(serializable_entry + '\n')

        return results
