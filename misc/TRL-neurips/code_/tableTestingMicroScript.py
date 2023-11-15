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
from utils import num_tokens_from_string


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

    def check(self, test: TestCase, answer: Any) -> bool:
        if test.TestType in ["ColumnLookupTests", "RowLookupTests"]:
            if isinstance(test.expect, list):
                expected_values = test.expect if isinstance(
                    test.expect, list) else [test.expect]
                expected_values = list(map(str, expected_values))
                matches = set(expected_values).intersection(set([answer]))
            else:
                print("Error: The expected Answer 'List' should be a list not string")
            return len(matches) > 0
        return str(test.expect) == answer

    def metric_pass_k(self, test: TestCase, answer: Any, k: int) -> float:
        if not isinstance(answer, list):
            raise ValueError("Answer should be a list for pass@k metric.")

        # Convert the test.expect to a list if it's not already
        expected_values = test.expect if isinstance(
            test.expect, list) else [test.expect]
        expected_values = list(map(str, expected_values))
        boolean_answers = [
            True if answer[i] in expected_values else False for i in range(len(answer))]
        combinations_k = list(combinations(boolean_answers, k))
        passed_at_k = 0
        # Calculate the pass@k metric
        for comb in combinations_k:
            if any(comb):
                passed_at_k += 1
        pass_at_k_percentage = (passed_at_k / len(combinations_k))*100

        return pass_at_k_percentage


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

        question = f"Can you reconstruct the table in a json 'index' format by deserializing the table above?"
        answer = df.to_json(orient='index', index=True)

        yield TestCase(question, answer, "TableReconstructionTest")


class TableColumnReorderTests(TestCaseGenerator):
    def generate(self, df, random_state=None):
        random_state = get_random_state(random_state)
        cols = list(df.columns)
        while True:
            random_state.shuffle(cols)
            new_df = df[cols]
            new_column_order = str(new_df.columns.to_list())
            question = f"""Can you reorder the table such that the column are in this new order {new_column_order}?
Return the reordered table in json 'index' format. """
            answer = new_df.to_json(orient='index', index=True)
            yield TestCase(question, answer, "ColumnShuffleTests")


class TableTransposeTests(TestCaseGenerator):
    def generate(self, df, random_state=None):

        question = f"""Can you transpose the table? Return the transposed table in json 'index' format."""
        answer = df.to_json(orient='index', index=True)
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
            get_column_name = [''.join(random.choices(
                string.ascii_letters + string.digits, k=np.random.randint(1, 11))) for _ in range(200)]
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
        yield df


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
                            replace=self.replace)


class TransposeTable(TableOperation):
    def modify(self, df, random_state=None):
        df = df.copy(deep=True)
        yield df.T


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

        yield new_df


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
            data += str(d) + " : " + str(df[d].to_list()) + ", "
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
        maxTok = 100
        num_n = 15
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

        answer, cache = openapi_call_completions(
            prompt, modelName="text-davinci-003", temp=temperature, maxTok=100, num_n=15, open_api_key=open_api_key)
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
                cache_path, f"micro_test_log_{formatted_datetime}.json")
            save_catch_file2 = os.path.join(
                cache_path2, f"micro_test_log_{formatted_datetime}.json")
            with open(save_catch_file, mode="w") as writer:
                pass

        for table_op in self.table_ops:

            for df_variant in itertools.islice(table_op.modify(df),
                                               per_table_op):
                df_variant_for_test = df_variant
                if table_op.__class__.__name__ in ["ColumnCluster", "SerializeTable"]:
                    df_variant_for_test = df

                for gen in self.test_gens:
                    if table_op.__class__.__name__ in ["OriginalData", "SequentialColumnNames", "TransposeTable", "SerializeTable"]:
                        no_of_test = per_test_gen*per_table_op
                    else:
                        no_of_test = per_test_gen

                    for test in itertools.islice(gen.generate(df_variant_for_test),
                                                 no_of_test):

                        for tab_format in self.table_formats:
                            temperature_list = [0]
                            for temp in temperature_list:

                                try:
                                    examples = gather_Examples_Prompt(
                                        tab_format, gen)
                                    prompt_cache_per_call = self.llm.get_prompt(
                                        examples, tab_format.formatting(df_variant), test.question, temp)

                                    answer, cache_per_call = self.llm.get_answer(examples, tab_format.formatting(
                                        df_variant), test.question, temp, self.open_api_key)
                                    result = gen.check(test, answer[0])
                                    pass_1 = gen.metric_pass_k(test, answer, 1)
                                    pass_3 = gen.metric_pass_k(test, answer, 3)
                                    pass_5 = gen.metric_pass_k(test, answer, 5)
                                    pass_10 = gen.metric_pass_k(
                                        test, answer, 10)
                                    pass_15 = gen.metric_pass_k(
                                        test, answer, 15)

                                    error = None

                                except Exception as Err:
                                    answer, cache_per_call = None, None
                                    result = None
                                    pass_1 = pass_3 = pass_5 = pass_10 = pass_15 = None
                                    error = str(Err)
                                    print(f"error {Err} encountered")
                                    error = str(Err)
                                    print(f"error {Err} encountered")

                                cache_all = {"model-Temperature": str(temp),
                                             "tab_format": str(tab_format.__class__.__name__),
                                             "table_op": str(table_op.__class__.__name__),
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
                                             "prompt_cache_per_call": prompt_cache_per_call,
                                             "LLMOutput": cache_per_call}
                                results.append((temp, tab_format.__class__.__name__, table_op.__class__.__name__, gen.__class__.__name__, test.question,
                                               test.expect, test.TestType, pass_1, pass_3, pass_5, pass_10, pass_15, test, answer, answer, result, error))
                                cacahe_all_dict.append(cache_all)
                                if save_cache:
                                    with open(save_catch_file, mode="a") as file:
                                        serializable_entry = json.dumps(
                                            cache_all, cls=CustomJSONEncoder)
                                        file.write(serializable_entry + '\n')

        if save_cache:
            with open(save_catch_file2, mode="w") as file:
                for result_entry in cacahe_all_dict:
                    serializable_entry = json.dumps(
                        result_entry, cls=CustomJSONEncoder)
                    file.write(serializable_entry + '\n')

        return results
