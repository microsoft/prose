# Copyright (c) Microsoft Corporation.  All rights reserved.

from prose.datainsights._assertion._disjunctive_assertions import DisjunctiveAssertion
from prose.datainsights._assertion._decision_tree_assertions import (
    DecisionTreeAssertion,
)
from ._assertion._assertion_helper import _AssertionType, Assertion

# ---------------------------------------------------------------
# Class DataAssertionImpl: Conjunctive collection of different assertions
# ---------------------------------------------------------------
class DataAssertionImpl(Assertion):
    def __init__(self, df):
        super().__init__(_AssertionType.MIXED, df, list(df.columns))

        self.assertion_objects = []
        self.constrained_invariants = []

    def add(self, assertion):
        self.constrained_invariants.extend(assertion.constrained_invariants)
        self.assertion_objects.append(assertion)
        self.populate(self.constrained_invariants)

    def evaluate(self, df, monitoring_options):
        result = super().evaluate(df, monitoring_options)
        result.compute_explanation = (
            "explanation" in monitoring_options and monitoring_options["explanation"]
        )
        if (
            result.compute_explanation
            and self.get(_AssertionType.DECISION_TREE_ASSERTION) is not None
        ):
            result.level_wise_violation = self.get(
                _AssertionType.DECISION_TREE_ASSERTION
            ).get_level_wise_explanation(df, result)

        return result

    def get(self, type):
        for assertion_object in self.assertion_objects:
            if assertion_object.type == type:
                return assertion_object
        return None


def learn_assertions_impl(
    df,
    max_col_in_slice,
    slice_col_overlap,
    max_row_in_slice,
    use_const_term,
    standardize_pca,
    learn_disjunctive,
    learn_decision_tree,
    max_unique_value_per_categorical_attribute,
    max_allowed_number_of_constraint_per_attribute,
    partition_on_categorical_attribute_only,
    max_tree_depth,
    max_self_violation,
    cross_validate,
    n_fold,
    assertion_improvement_factor,
):

    if not learn_decision_tree:
        # This is equivalent to plain PCA assertion learning
        learn_decision_tree = True
        max_tree_depth = 1

    data_assertion = DataAssertionImpl(df)

    if learn_decision_tree:
        dt_assertion = DecisionTreeAssertion.learn(
            df=df,
            max_col_in_slice=max_col_in_slice,
            slice_col_overlap=slice_col_overlap,
            max_row_in_slice=max_row_in_slice,
            use_const_term=use_const_term,
            standardize_pca=standardize_pca,
            max_unique_value_per_categorical_attribute=max_unique_value_per_categorical_attribute,
            max_allowed_number_of_constraint_per_attribute=max_allowed_number_of_constraint_per_attribute,
            partition_on_categorical_attribute_only=partition_on_categorical_attribute_only,
            max_tree_depth=max_tree_depth,
            max_self_violation=max_self_violation,
            cross_validate=cross_validate,
            n_fold=n_fold,
            assertion_improvement_factor=assertion_improvement_factor,
        )
        data_assertion.add(dt_assertion)

    if learn_disjunctive:
        dis_assertion = DisjunctiveAssertion.learn(
            df=df,
            max_col_in_slice=max_col_in_slice,
            slice_col_overlap=slice_col_overlap,
            max_row_in_slice=max_row_in_slice,
            use_const_term=use_const_term,
            standardize_pca=standardize_pca,
            max_unique_value_per_categorical_attribute=max_unique_value_per_categorical_attribute,
            max_allowed_number_of_constraint_per_attribute=max_allowed_number_of_constraint_per_attribute,
            partition_on_categorical_attribute_only=partition_on_categorical_attribute_only,
            max_self_violation=max_self_violation,
            cross_validate=cross_validate,
            n_fold=n_fold,
        )
        data_assertion.add(dis_assertion)

    return data_assertion
