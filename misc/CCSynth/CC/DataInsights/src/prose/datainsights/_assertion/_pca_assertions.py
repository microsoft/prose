# Copyright (c) Microsoft Corporation.  All rights reserved.

import numpy as np
import pandas as pd
import math
from statistics import median
from ._assertion_helper import (
    _AssertionType,
    Assertion,
    ConstrainedInv,
    ConjunctiveConstraint,
)

# TODO: these constants should preferably be computed based on the data means in future.
EPS = 1e-6
COEFFICIENT_PRECISION = 4
LARGE_VALUE = 1e6

# ---------------------------------------------------------------
# Class PcaAssertion: a collection of single pca invariants.
#    A single pca invariant consists of a function (from a dataframe row) to a (vector of)
#    real value(s), coupled with a (vector of) mean and (vector of) standard deviation
#    for the computed value.  When we validate a row against this invariant, we get a
#    degree of violation from 0 to 1, with 0 indicating no violation, and 1 indicating
#    a definite violation.
# ---------------------------------------------------------------
class PcaAssertion(Assertion):
    @staticmethod
    def learn(df, **kwargs):
        return _PcaAssertionBuilderImpl(df=df, **kwargs).learn()

    def __init__(
        self,
        transformed_df,
        display_func,
        inv_matrix,
        inv_matrix_all,
        transform_func,
        df,
        features,
    ):
        Assertion.__init__(self, _AssertionType.PCA_ASSERTION, df, features)
        self._transform = transform_func
        values = transformed_df.dot(inv_matrix)
        self.mean = values.mean(axis=0, skipna=True)
        self.std_dev = values.std(axis=0, skipna=True, ddof=0)
        self.std_dev_all = transformed_df.dot(inv_matrix_all).std(
            axis=0, skipna=True, ddof=0
        )
        self.std_dev_inv = self.std_dev.apply(
            lambda x: 1.0 / x if abs(x) > EPS else LARGE_VALUE
        )
        self.mins = values.min(axis=0, skipna=True)
        self.maxs = values.max(axis=0, skipna=True)
        self.number_of_features = transformed_df.shape[1]
        self.number_of_invs = values.shape[1]
        self._valid = self.number_of_invs > 0
        self.display_func = display_func
        self.display_str_list = None
        self.inv_matrix = inv_matrix

        super().populate([ConstrainedInv(ConjunctiveConstraint([]), self)])

    def size(self):
        """Return number of sub-invariants learnt"""
        return self.number_of_invs

    def _get_name(self, index):
        if self.display_str_list is None:
            self.display_str_list = self.display_func()
        if not self._valid:
            return "None"
        return "Eigen invariant: {0}, mean: {1:.2g}, stddev: {2:.2g}, min: {3:.2g}, max: {4:.2g}".format(
            self.display_str_list[index],
            self.mean[index],
            self.std_dev[index],
            self.mins[index],
            self.maxs[index],
        )


# ---------------------------------------------------------------
# Class _PcaAssertionBuilderImpl
# ---------------------------------------------------------------
class _PcaAssertionBuilderImpl:
    """
        For internal use only
    """

    def __init__(
        self,
        df,
        max_col_in_slice,
        slice_col_overlap,
        max_row_in_slice,
        use_const_term,
        standardize_pca,
        max_self_violation,
        cross_validate,
        n_fold,
        num_invs_to_return,
        eigen_value_threshold=1,
        variance_ratio_factor=1,
    ):
        self._input_df = df
        self._max_col_in_slice = max_col_in_slice
        self._slice_col_overlap = slice_col_overlap
        self._max_row_in_slice = max_row_in_slice
        self._use_const_term = use_const_term
        self._standardize_pca = standardize_pca
        self._max_self_violation = max_self_violation
        self._cross_validate = cross_validate
        self._n_fold = n_fold
        self._num_invs_to_return = num_invs_to_return
        self._eigen_value_threshold = eigen_value_threshold
        self._variance_ratio_factor = variance_ratio_factor

    def learn(self):
        transformed_df = self._transform(self._input_df)
        transformed_column_names = transformed_df.columns

        invs = pd.DataFrame(index=transformed_column_names)
        invs_all = pd.DataFrame(index=transformed_column_names)

        # break the data into overlapping chunks of self._max_col_in_slice columns
        # pick self._max_row_in_slice rows from the dataframe to learn the invariant, and check the rest...
        for df_slice, is_base in self._create_slices(transformed_df):
            # remove rows with nan values
            clean_df_slice = df_slice.dropna()
            if (
                is_base
                and not clean_df_slice.empty
                # number of rows should be more than twice as much as the number of (numerical) columns
                and clean_df_slice.shape[0] > 2 * clean_df_slice.shape[1]
            ):
                if self._cross_validate:
                    self._compute_best_num_of_assertions_to_learn(clean_df_slice)

                inv_vecs_df, inv_vecs_df_all = self._find_candidate_invariants(
                    clean_df_slice
                )
                invs = pd.concat(
                    (invs, inv_vecs_df),
                    axis=1,
                    ignore_index=True,
                    copy=False,
                    sort=False,
                )

                invs_all = pd.concat(
                    (invs_all, inv_vecs_df_all),
                    axis=1,
                    ignore_index=True,
                    copy=False,
                    sort=False,
                )
            else:
                break
        invs.fillna(0, inplace=True)
        invs_all.fillna(0, inplace=True)

        if invs.shape[1] > 0:
            advanced_inv_display = lambda: [
                " + ".join(
                    [
                        "{0}*{1}".format(
                            np.round(invs.loc[row, col], COEFFICIENT_PRECISION), row
                        )
                        for row in invs.index
                        if np.fabs(invs.loc[row, col]) > EPS
                    ]
                )
                for col in invs.columns
            ]
        else:
            advanced_inv_display = lambda: ["true"]

        return PcaAssertion(
            transformed_df=transformed_df,
            display_func=advanced_inv_display,
            inv_matrix=invs,
            inv_matrix_all=invs_all,
            transform_func=self._transform,
            df=self._input_df,
            features=list(self._input_df.columns),
        )

    def _transform(self, df):
        """Only for internal use."""
        numerical_df = df.select_dtypes(include="number")
        if self._use_const_term:
            numerical_df = numerical_df.assign(_one=lambda x: 1)
        return numerical_df

    def _create_slices(self, data_df):
        """ Slice the data by column and by row based on self._max_col_in_slice and self._max_row_in_slice constants.
        Ideally, self._max_row_in_slice should be 2 to 2.5 times self._max_col_in_slice.
        We pick the top self._max_row_in_slice rows from df.  For columns, we pick self._max_col_in_slice column
        slices, where the adjacent slices overlaps by _slice_col_overlap columns.  For internal use only. """
        column_names = data_df.columns
        (n_rows, n_cols) = data_df.shape
        row_idx_low, row_idx_high = 0, min(n_rows, self._max_row_in_slice)
        col_idx_low, col_idx_high = 0, min(n_cols, self._max_col_in_slice)
        base = True
        while row_idx_high <= n_rows or col_idx_high <= n_cols:
            yield data_df[row_idx_low:row_idx_high].filter(
                items=column_names[col_idx_low:col_idx_high], axis=1
            ), base
            if col_idx_high < n_cols:
                col_idx_low = col_idx_high - self._slice_col_overlap
                col_idx_high = min(col_idx_low + self._max_col_in_slice, n_cols)
            elif row_idx_high < n_rows:
                col_idx_low, col_idx_high = 0, min(n_cols, self._max_col_in_slice)
                row_idx_low, row_idx_high = (
                    row_idx_high,
                    min(row_idx_high + self._max_row_in_slice, n_rows),
                )
                base = False
            else:
                break

    # Use k-fold cross validation to decide on the optimal number of invariants to learn
    def _compute_best_num_of_assertions_to_learn(self, df):
        rows_per_fold = int(df.shape[0] / self._n_fold)
        splitted_df = [
            df.iloc[i * rows_per_fold : (i + 1) * rows_per_fold]
            for i in range(self._n_fold - 1)
        ]
        splitted_df.append(df.iloc[(self._n_fold - 1) * rows_per_fold :])

        valid_ks = []
        validation_split = (
            df
        )  # We should validate on the entire data-frame, outlier can reside within the training data as well

        for fold in range(self._n_fold):
            train_split = pd.concat(splitted_df[:fold] + splitted_df[fold + 1 :])

            high = len(df.columns)
            low = 0
            k = 0
            while high >= low:
                mid = int(math.floor((high + low) / 2))
                pca_assertion_now = _PcaAssertionBuilderImpl(
                    df=train_split,
                    max_col_in_slice=self._max_col_in_slice,
                    slice_col_overlap=self._slice_col_overlap,
                    max_row_in_slice=self._max_row_in_slice,
                    use_const_term=self._use_const_term,
                    standardize_pca=self._standardize_pca,
                    max_self_violation=self._max_self_violation,
                    cross_validate=False,
                    n_fold=self._n_fold,
                    num_invs_to_return=mid,
                ).learn()
                violation_result = pca_assertion_now.evaluate(validation_split, {})
                if violation_result.worst_violation <= self._max_self_violation:
                    low = mid + 1
                    k = mid
                else:
                    high = mid - 1

            valid_ks.append(k)

        best_k = int(math.floor(median(valid_ks)))

        self._cross_validate = False
        self._num_invs_to_return = best_k

    def _find_candidate_invariants(self, df):
        """return candidate invariants for dataset X. For internal use only."""
        X = df.to_numpy()

        if self._standardize_pca:
            # Perform standardization before PCA
            mn = np.mean(X, axis=0)
            X = X / np.maximum(np.ones_like(mn), mn)

        XT = np.transpose(X)
        XTX = np.matmul(XT, X)

        # eigenvalue computation can throw an exception due to non-convergence
        try:
            eigen_values, eigen_vectors = np.linalg.eigh(XTX)
            invs_all = pd.DataFrame(eigen_vectors, index=df.columns)
            if self._num_invs_to_return is not None:
                invs_final = pd.DataFrame(
                    eigen_vectors[:, : self._num_invs_to_return], index=df.columns
                )
            else:
                # Use fixed thresholds to decide on good invariants.

                # Since XTX is a PSD, eigen values cannot be negative.
                # However, eigen_values can be small negative numbers due to numerical instability,
                # so, casting to EPS for future numerical stability during computing log_variance_ratio
                eigen_values = np.maximum(
                    np.zeros_like(eigen_values) + EPS, eigen_values
                )
                variances = df.var(ddof=0).to_numpy()
                expected_variances = np.matmul(variances, np.square(eigen_vectors))

                # expected_variance should always be positive
                expected_variances = np.maximum(
                    np.zeros_like(expected_variances) + EPS, expected_variances
                )
                good_indices = [
                    i
                    for i in range(len(eigen_values))
                    if eigen_values[i] < self._eigen_value_threshold
                    or np.log(eigen_values[i]) - np.log(expected_variances[i])
                    < np.log(self._variance_ratio_factor)
                ]
                invs_final = pd.DataFrame(
                    eigen_vectors[:, good_indices], index=df.columns
                )

        except np.linalg.LinAlgError:
            invs_all = pd.DataFrame([], index=df.columns)
            invs_final = pd.DataFrame([], index=df.columns)

        return invs_final, invs_all

    def __repr__(self):
        return """PcaAssertionBuilder
==================
input dataframe:
{0}
Parameters to use when assertion learning:
maximum columns in a slice = {1}
maximum rows in a slice = {2}
maximum overlapping columns = {3}
use constant term in linear assertions = {4}
standardize PCA = {5}
max self violation = {6}
cross validate = {7}
number of folds = {8}
number of invs to return = {9}
eigen value threshold = {10}
variance ratio factor = {11}""".format(
            self._input_df,
            self._max_col_in_slice,
            self._max_row_in_slice,
            self._slice_col_overlap,
            self._use_const_term,
            self._standardize_pca,
            self._max_self_violation,
            self._cross_validate,
            self._n_fold,
            self._num_invs_to_return,
            self._eigen_value_threshold,
            self._variance_ratio_factor,
        )
