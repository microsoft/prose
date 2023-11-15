from typing import Any, Union
import pandas as pd
import numpy as np
import datetime


def parse_cell_value(reference_cell: Any, other_cell: Any) -> Any:
    """
    Parses other_cell as reference_cell type if other_cell is string
    """
    if not isinstance(other_cell, str):
        return other_cell
    try:
        if isinstance(reference_cell, bool):
            # check bool first since bool isinstance of int as well
            # tolerate capitalization differences for boolean
            return other_cell.lower() == "true"
        elif isinstance(reference_cell, int) or isinstance(reference_cell, float):
            # always parse as float to be safe with decimal points
            return float(other_cell)
        elif isinstance(reference_cell, datetime.date):
            # best guess parse
            return pd.to_datetime(other_cell).date()
        elif isinstance(reference_cell, datetime.datetime):
            # best guess parse
            return pd.to_datetime(other_cell).to_pydatetime()
        else:
            # fall back to str
            return other_cell
    except ValueError:
        # fall back to str if fails to parse
        return other_cell


def cells_are_equal(cell1: Any, cell2: Any) -> int:
    try:
        if isinstance(cell1, (bool, str, datetime.date, datetime.datetime, pd.Timestamp)):
            return cell1 == cell2
        elif isinstance(cell1, (float, int, np.number)):
            return np.allclose(cell1, cell2)
        elif np.isnan(cell1):
            return np.isnan(cell2)
        elif np.isinf(cell1):
            return np.isinf(cell2) and (np.sign(cell1) == np.sign(cell2))
        else:
            # fall back
            return cell1 == cell2
    except:
        # try again in case it was numpy issues
        try:
            return cell1 == cell2
        except:
            return False


def make_two_dim(obj):
    if obj.ndim == 2:
        return obj
    if obj.ndim == 1:
        return obj.reshape(-1, 1)
    else:
        raise Exception("Expect object with 1 or 2 dimensions")


def compare_per_cell(
        reference_df: pd.DataFrame,
        other_df: pd.DataFrame,
        type_reference_df: pd.DataFrame = None,
        count_header_and_index: bool = True,
        return_fraction: bool = True,
):
    """
    Per-cell comparison with respect to reference_df (i.e.
    (other_df matches) / (reference_df cells)
    ).

    We cast other_df cells to match the type in type_reference_df (default to 
    reference_df if not provided)

    We treat header row and column indices as just other cells.
    """
    if type_reference_df is None:
        type_reference_df = reference_df

    as_dataframe = isinstance(reference_df, pd.DataFrame)
    if as_dataframe:
        ref_vals = reference_df.reset_index(drop=True)
        other_vals = other_df.reset_index(drop=True)
        type_ref_vals = type_reference_df.reset_index(drop=True)
        # iterate over reference dimensions
        rows_range = range(ref_vals.shape[0])
        cols_range = ref_vals.columns
        def lookup_ij(df, i, j): return df.loc[i, j]
    else:
        ref_vals = make_two_dim(reference_df.values)
        other_vals = make_two_dim(other_df.values)
        type_ref_vals = make_two_dim(type_reference_df.values)
        nrows, ncols = ref_vals.shape
        rows_range = range(nrows)
        cols_range = range(ncols)
        def lookup_ij(mat, i, j): return mat[i, j]

    success_ct = 0
    total_ct = 0

    for i in rows_range:
        for j in cols_range:
            # print(i,j)
            # print(ref_vals.index)
            ref_cell = lookup_ij(ref_vals, i, j)
            total_ct += 1
            try:
                other_cell = lookup_ij(other_vals, i, j)
                type_ref_cell = lookup_ij(type_ref_vals, i, j)
            except (IndexError, KeyError):
                # failed since out of bounds
                # print("failed")
                continue
            other_cell_parsed = parse_cell_value(type_ref_cell, other_cell)
            success_ct += int(cells_are_equal(ref_cell, other_cell_parsed))

    if count_header_and_index:
        # compare header
        ref_header_row = reference_df.columns
        other_header_row = other_df.columns
        add_to_success, add_to_total = compare_per_cell(
            ref_header_row, other_header_row, count_header_and_index=False, return_fraction=False)
        total_ct += add_to_total
        success_ct += add_to_success

        # compare column index
        ref_index_col = reference_df.index
        other_index_col = other_df.index
        add_to_success, add_to_total = compare_per_cell(
            ref_index_col, other_index_col, count_header_and_index=False, return_fraction=False)
        total_ct += add_to_total
        success_ct += add_to_success

    if return_fraction:
        return float(success_ct) / float(total_ct)
    else:
        return success_ct, total_ct


if __name__ == "__main__":
    data1 = {'A': [1, 2, 3],
             'B': [True, False, True],
             'C': ['apple', 'banana', 'cherry'],
             'D': [datetime.date(2023, 1, 1), datetime.date(2023, 2, 2), datetime.date(2023, 3, 3)]}
    df1 = pd.DataFrame(data1)

    # Create the second DataFrame with overlapping values
    data2 = {'A': [1, 5, 3],
             'B': [True, True, False],
             'C': ['apple', 'elephant', 'cherry'],
             'D': [datetime.date(2023, 1, 1), datetime.date(2023, 2, 2), datetime.date(2023, 3, 3)]}
    df2 = pd.DataFrame(data2)

    # equal to self
    assert compare_per_cell(df1, df1) == 1.0
    assert compare_per_cell(df2, df2) == 1.0
    # empty
    assert compare_per_cell(df1, pd.DataFrame()) == 0.0
    assert compare_per_cell(
        df1, df1.iloc[:0], count_header_and_index=False) == 0.0
    assert compare_per_cell(
        df1.iloc[:0], df1, count_header_and_index=False, return_fraction=False) == (0, 0)
    # both directions
    assert compare_per_cell(df1, df2, type_reference_df=df1,
                            count_header_and_index=False, return_fraction=False) == (8, 12)
    assert compare_per_cell(df2, df1, type_reference_df=df2,
                            count_header_and_index=False, return_fraction=False) == (8, 12)
    # subsets
    df1_subset = df1[["C", "D"]]
    assert compare_per_cell(df1_subset, df2, type_reference_df=df1_subset,
                            count_header_and_index=False, return_fraction=False) == (5, 6)
    assert compare_per_cell(df2, df1_subset, type_reference_df=df2,
                            count_header_and_index=False, return_fraction=False) == (5, 12)
