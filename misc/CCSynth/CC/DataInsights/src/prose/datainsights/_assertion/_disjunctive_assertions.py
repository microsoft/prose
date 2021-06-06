# Copyright (c) Microsoft Corporation.  All rights reserved.

from prose.datainsights._assertion._pca_assertions import PcaAssertion
from prose.datainsights._assertion._assertion_helper import (
    _RelationalOperators,
    _AssertionType,
    SingleConstraint,
    ConstrainedInv,
    Assertion,
)

# ---------------------------------------------------------------
# Class DisjunctiveAssertion: A wrapper around List of ConstrainedInv
# ---------------------------------------------------------------
class DisjunctiveAssertion(Assertion):
    @staticmethod
    def learn(df, **kwargs):
        return _DisjunctiveAssertionBuilderImpl(df=df, **kwargs).learn()

    def __init__(self, df, features, constrained_invariants):
        super().__init__(_AssertionType.DISJUNCTIVE_ASSERTION, df, features)
        self.populate(constrained_invariants)


# ---------------------------------------------------------------
# Class _DisjunctiveAssertionBuilderImpl
# ---------------------------------------------------------------
class _DisjunctiveAssertionBuilderImpl:
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
        max_unique_value_per_categorical_attribute,
        max_allowed_number_of_constraint_per_attribute,
        partition_on_categorical_attribute_only,
        max_self_violation,
        cross_validate,
        n_fold,
    ):
        self._input_df = df
        self._max_col_in_slice = max_col_in_slice
        self._slice_col_overlap = slice_col_overlap
        self._max_row_in_slice = max_row_in_slice
        self._use_const_term = use_const_term
        self._standardize_pca = standardize_pca
        self._max_unique_value_per_categorical_attribute = (
            max_unique_value_per_categorical_attribute
        )
        self._max_allowed_number_of_constraint_per_attribute = (
            max_allowed_number_of_constraint_per_attribute
        )
        self._partition_on_categorical_attribute_only = (
            partition_on_categorical_attribute_only
        )
        self._max_self_violation = max_self_violation
        self._cross_validate = cross_validate
        self._n_fold = n_fold

    def learn(self):
        df = self._input_df
        categorical_columns = [
            column
            for column, dtype in df.dtypes.iteritems()
            if (
                (not self._partition_on_categorical_attribute_only or dtype == object)
                and len(df[column].unique()) > 1
                and (
                    self._max_unique_value_per_categorical_attribute is None
                    or len(df[column].unique())
                    <= self._max_unique_value_per_categorical_attribute
                )
            )
        ]
        constrained_invariants = []
        for column in categorical_columns:
            constrained_invariants.extend(self.learn_single_column(column))
        return DisjunctiveAssertion(
            df=self._input_df,
            features=list(self._input_df.columns),
            constrained_invariants=constrained_invariants,
        )

    def learn_single_column(self, column):
        df = self._input_df
        unique_values = list(df[column].unique())
        constrained_invariants = []

        for value in unique_values:
            constraint = SingleConstraint(
                column_name=column,
                column_value=value,
                relational_op=_RelationalOperators.EQUAL,
            )
            constrained_df = constraint.apply(self._input_df)

            assertion = PcaAssertion.learn(
                df=constrained_df,
                max_col_in_slice=self._max_col_in_slice,
                slice_col_overlap=self._slice_col_overlap,
                max_row_in_slice=self._max_row_in_slice,
                use_const_term=self._use_const_term,
                standardize_pca=self._standardize_pca,
                max_self_violation=self._max_self_violation,
                cross_validate=self._cross_validate,
                n_fold=self._n_fold,
                num_invs_to_return=None,
            )

            c_inv = ConstrainedInv(constraint=constraint, data_assertion=assertion)
            if c_inv.is_valid():
                constrained_invariants.append(c_inv)

        return constrained_invariants

    def __repr__(self):
        return """DisjunctiveAssertionBuilder
    ==================
    input dataframe:
    {0}
    Parameters to use when assertion learning:
    maximum columns in a slice = {1}
    maximum rows in a slice = {2}
    maximum overlapping columns = {3}
    use constant term in linear assertions = {4}
    standardize PCA = {5}
    max unique value per categorical attribute = {6}
    max allowed number of constraint per attribute = {7}
    partition on categorical attribute only = {8}
    maximum allowed self violation = {9}
    cross validate = {10}
    number of folds = {11}""".format(
            self._input_df,
            self._max_col_in_slice,
            self._max_row_in_slice,
            self._slice_col_overlap,
            self._use_const_term,
            self._standardize_pca,
            self._max_unique_value_per_categorical_attribute,
            self._max_allowed_number_of_constraint_per_attribute,
            self._partition_on_categorical_attribute_only,
            self._max_self_violation,
            self._cross_validate,
            self._n_fold,
        )
