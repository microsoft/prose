import tiktoken
from io import StringIO
import ast
import pandas as pd


def Convert_back_to_df(Format: str, data_string: str):
    if Format == "JsonFormat":
        return pd.DataFrame(ast.literal_eval(data_string)).T
    if Format == "MarkdownFormat":
        extracted_df = pd.read_csv(
            StringIO(data_string.replace(' ', '')),  # Get rid of whitespaces
            sep='|',
            index_col=1
        ).dropna(
            axis=1,
            how='all'
        ).iloc[1:]
        extracted_df_ = extracted_df.map(
            lambda x: x.strip() if isinstance(x, str) else x)
        extracted_df_.columns = [col.strip() for col in extracted_df.columns]
        return extracted_df_
    if Format == "DFloaderFormat":
        start_inx = data_string.find("pd.DataFrame(")+len("pd.DataFrame(")
        stop_idx = data_string.find(", index=[") 
        string_dict = data_string[start_inx:stop_idx]
        string_dict
        dict_data_part = ast.literal_eval(data_string[start_inx:stop_idx])
        list_index = ast.literal_eval(data_string[stop_idx+len(", index="):-1])
        return pd.DataFrame(dict_data_part, index=list_index)

    if Format == "DataMatrixFormat":
        Matrix = ast.literal_eval(data_string.replace("\n", ""))
        matrix_df = pd.DataFrame(Matrix)
        matrix_df.index = matrix_df[0].to_list()
        matrix_df = matrix_df.drop(columns=[0])
        matrix_df.columns = matrix_df.iloc[0, :].tolist()
        matrix_df = matrix_df.iloc[1:, :]
        return matrix_df

    if Format == "CommaSeparatedFormat":
        dff = pd.read_csv(StringIO(data_string), sep=",")
        index_col = dff.columns[0]
        dff.index = dff[index_col].to_list()
        dff = dff.drop(columns=[index_col])
        return dff

    if Format == "TabSeparatedFormat":
        dff = pd.read_csv(StringIO(data_string), sep="\t")
        index_col = dff.columns[0]
        dff.index = dff[index_col].to_list()
        dff = dff.drop(columns=[index_col])
        return dff

    if Format in ["HTMLNoSpaceFormat", "HTMLFormat"]:
        dff = pd.read_html(StringIO(data_string))[0]
        index_col = dff.columns[0]
        dff.index = dff[index_col].to_list()
        dff = dff.drop(columns=[index_col])
        return dff


def num_tokens_from_string(string: str, encoding_name: str = "p50k_base") -> int:
    """Returns the number of tokens in a text string."""
    encoding = tiktoken.get_encoding(encoding_name)
    num_tokens = len(encoding.encode(string))
    return num_tokens


num_tokens_from_string("tiktoken is great!", "cl100k_base")


def stringify_serialized_df(serialized_df: pd.DataFrame):
    return "\n".join([v[0] for v in serialized_df.values])
