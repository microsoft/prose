# Copyright (c) Microsoft Corporation.  All rights reserved.

import numpy as np
import pandas as pd
from ._pca_assertions import PcaAssertion
from prose.datainsights._assertion._assertion_helper import (
    _RelationalOperators,
    _AssertionType,
    SingleConstraint,
    ConjunctiveConstraint,
    ConstrainedInv,
    Assertion,
)
import threading

# ---------------------------------------------------------------
# Class BestColumnForSplit: contains an implementations of a function that
# returns the best column to use for splitting while building a decision tree
# ---------------------------------------------------------------
class BestColumnForSplit:
    def __init__(
        self,
        df,
        cat_columns,
        child_assertion_increasing_factor,
        min_cat_cols_to_check,
        max_col_in_slice,
        slice_col_overlap,
        max_row_in_slice,
        use_const_term,
        standardize_pca,
        max_self_violation,
        cross_validate,
        n_fold,
        assertion_improvement_factor,
    ):
        self.df = df
        self.cat_columns = cat_columns
        self.child_assertion_increasing_factor = child_assertion_increasing_factor
        self.min_cat_cols_to_check = min_cat_cols_to_check
        self.max_col_in_slice = max_col_in_slice
        self.slice_col_overlap = slice_col_overlap
        self.max_row_in_slice = max_row_in_slice
        self.use_const_term = use_const_term
        self.standardize_pca = standardize_pca
        self.max_self_violation = max_self_violation
        self.cross_validate = cross_validate
        self.n_fold = n_fold
        self.assertion_improvement_factor = assertion_improvement_factor

    """
        This approach splits the data across each categorical column, and
        checks the quality of new assertions.
    """

    def assertion_learner_thread(self, col, value):
        cur_assertion = PcaAssertion.learn(
            df=self.df[self.df[col] == value],
            max_col_in_slice=self.max_col_in_slice,
            slice_col_overlap=self.slice_col_overlap,
            max_row_in_slice=self.max_row_in_slice,
            use_const_term=self.use_const_term,
            standardize_pca=self.standardize_pca,
            max_self_violation=self.max_self_violation,
            cross_validate=self.cross_validate,
            n_fold=self.n_fold,
            num_invs_to_return=None,
        )
        with self.lock:
            if not cur_assertion.is_valid():
                return

            if cur_assertion.get_inv_count() > self.max_inv_count:
                self.max_inv_count = cur_assertion.get_inv_count()

            if col not in self.child_count:
                self.child_count[col] = 1
                self.all_std_devs[col] = np.array(cur_assertion.std_dev_all).reshape(
                    (1, len(cur_assertion.std_dev_all))
                )
            else:
                self.child_count[col] += 1
                self.all_std_devs[col] = np.vstack(
                    (self.all_std_devs[col], cur_assertion.std_dev_all)
                )

    def get_best_column_from_partitions(self):
        root_assertion = PcaAssertion.learn(
            df=self.df,
            max_col_in_slice=self.max_col_in_slice,
            slice_col_overlap=self.slice_col_overlap,
            max_row_in_slice=self.max_row_in_slice,
            use_const_term=self.use_const_term,
            standardize_pca=self.standardize_pca,
            max_self_violation=self.max_self_violation,
            cross_validate=self.cross_validate,
            n_fold=self.n_fold,
            num_invs_to_return=None,
        )
        self.max_inv_count = 0
        self.all_std_devs = dict()
        self.child_count = dict()
        self.lock = threading.Lock()

        assertion_finding_thread = []

        for col in self.cat_columns:
            number_of_unique_values = len(self.df[col].unique())
            if number_of_unique_values > 1:
                for value in self.df[col].unique():
                    assertion_finding_thread.append(
                        threading.Thread(self.assertion_learner_thread(col, value))
                    )

        for t in assertion_finding_thread:
            t.start()

        for t in assertion_finding_thread:
            t.join()

        if self.max_inv_count == 0:
            best_col = None
        else:
            # "self.max_inv_count" is the maximum number of possible invariant in any children across any column split.
            # Considering the top "self.max_inv_count" invariants at the root and at all the children,
            # we pick the split that
            #   (1) minimizes the avg std_dev over all partitions(the lower avg std_devs are, the better)
            #   (2) the "improvement" of (new) avg std_dev, compared to the root's (old) avg std_dev is "significant".
            # We consider an improvement significant if
            #   new_avg_std  <= self.assertion_improvement_factor * avg_std_per_assertion_at_root

            avg_std_per_assertion_at_root = sum(
                root_assertion.std_dev_all[: self.max_inv_count]
            ) / float(self.max_inv_count)
            best_col = None
            best_avg_std_per_assertion = avg_std_per_assertion_at_root
            for col in self.all_std_devs:
                avg_std_per_assertion_at_this_split = np.sum(
                    self.all_std_devs[col][:, : self.max_inv_count]
                ) / float(self.child_count[col] * self.max_inv_count)
                if avg_std_per_assertion_at_this_split < best_avg_std_per_assertion:
                    best_col, best_avg_std_per_assertion = (
                        col,
                        avg_std_per_assertion_at_this_split,
                    )
            if (
                best_avg_std_per_assertion
                > self.assertion_improvement_factor * avg_std_per_assertion_at_root
                and root_assertion.get_inv_count() > 0
            ):
                # We are not seeing enough improvement in assertion quality with any splitting,
                # where we already learnt some assertion at the root.
                # So, rather not split
                best_col = None

        return best_col


# ---------------------------------------------------------------
# Class ViolationExplanation: Explains violation within the decision tree
# ---------------------------------------------------------------
class ViolationExplanation:
    def __init__(
        self, constraint, violation, nRows, valid, minimum_violation_threshold=1e-2
    ):
        self.constraint = constraint
        self.absolute_violation = violation
        self.nRows = nRows
        self.valid = valid
        self.fraction_violation = None
        self.children = []
        self.minimum_violation_threshold = minimum_violation_threshold

    def add_child(self, child):
        self.children.append(child)

    def process(self, fraction_violation):
        self.fraction_violation = fraction_violation
        if (
            len(self.children) > 0
            and self.fraction_violation >= self.minimum_violation_threshold
        ):
            total_children_violation = np.sum(
                np.array([child.absolute_violation for child in self.children])
            )
            if total_children_violation < self.minimum_violation_threshold:
                self.children = []
                return
            for child in self.children:
                child_fraction_violation = self.fraction_violation * (
                    child.absolute_violation / float(total_children_violation)
                )
                child.process(child_fraction_violation)

    def get_details(self):
        explanation = dict()
        explanation["constraint"] = self.constraint.get_name()
        explanation["number_of_rows"] = self.nRows
        explanation["fraction_of_violation_explained"] = round(
            self.fraction_violation, 2
        )
        if len(self.children) > 0:
            children_explanation_list = [
                child.get_details()
                for child in self.children
                if child.fraction_violation >= self.minimum_violation_threshold
            ]
            if len(children_explanation_list) > 0:
                explanation["children"] = children_explanation_list
        return explanation


# ---------------------------------------------------------------
# Class _Node: The decision tree that stores recursive constraints and corresponding assertions
# Constructor of this class is recursive.
# ---------------------------------------------------------------
class _Node:
    def __init__(self, df, node_depth, constraint, parameters):
        self.df = df
        self.node_depth = node_depth
        self.constraint = constraint
        self.parameters = parameters

        self.max_tree_depth = self.parameters.max_tree_depth
        self.max_unique_value_per_categorical_attribute = (
            self.parameters.max_unique_value_per_categorical_attribute
        )
        self.partition_on_categorical_attribute_only = (
            self.parameters.partition_on_categorical_attribute_only
        )

        self.df = constraint.apply(self.df)
        self.node_assertion = None
        self.children = []
        self.starting_invariant_id = None

        self.process()

        self.number_of_invs = 0
        self.constrained_invariants = []
        self.collect_all_child_assertions()

        self.valid = self.number_of_invs > 0

        if self.valid:
            self.starting_invariant_id = self.constrained_invariants[0].id

    def process(self):
        # Don't grow too deep trees
        if self.node_depth > self.max_tree_depth:
            return

        self.node_assertion = PcaAssertion.learn(
            df=self.df,
            max_col_in_slice=self.parameters.max_col_in_slice,
            slice_col_overlap=self.parameters.slice_col_overlap,
            max_row_in_slice=self.parameters.max_row_in_slice,
            use_const_term=self.parameters.use_const_term,
            standardize_pca=self.parameters.standardize_pca,
            max_self_violation=self.parameters.max_self_violation,
            cross_validate=self.parameters.cross_validate,
            n_fold=self.parameters.n_fold,
            num_invs_to_return=None,
        )

        # Too few data points to learn anything useful
        if len(self.node_assertion.std_dev_all) == 0:
            return

        if self.node_depth + 1 > self.max_tree_depth:
            return

        # Split
        categorical_columns = [
            column
            for column, dtype in self.df.dtypes.iteritems()
            if (
                not self.partition_on_categorical_attribute_only
                or not np.issubdtype(dtype, np.number)
            )
            and (
                self.max_unique_value_per_categorical_attribute is None
                or len(self.df[column].unique())
                <= self.max_unique_value_per_categorical_attribute
            )
        ]

        if len(categorical_columns) == 0:
            return

        best_col = BestColumnForSplit(
            df=self.df,
            cat_columns=categorical_columns,
            child_assertion_increasing_factor=2,
            min_cat_cols_to_check=10,
            max_col_in_slice=self.parameters.max_col_in_slice,
            slice_col_overlap=self.parameters.slice_col_overlap,
            max_row_in_slice=self.parameters.max_row_in_slice,
            use_const_term=self.parameters.use_const_term,
            standardize_pca=self.parameters.standardize_pca,
            max_self_violation=self.parameters.max_self_violation,
            cross_validate=self.parameters.cross_validate,
            n_fold=self.parameters.n_fold,
            assertion_improvement_factor=self.parameters.assertion_improvement_factor,
        ).get_best_column_from_partitions()

        if best_col is None:
            return

        for value in self.df[best_col].unique():
            cur_constraint = ConjunctiveConstraint(
                single_constraints=[
                    SingleConstraint(
                        column_name=best_col,
                        column_value=value,
                        relational_op=_RelationalOperators.EQUAL,
                    )
                ]
            )
            child = _Node(
                self.df,
                self.node_depth + 1,
                constraint=cur_constraint,
                parameters=self.parameters,
            )
            if child.number_of_invs > 0:
                self.children.append(child)

    def __repr__(self):
        self.name = "-" * self.node_depth
        self.name += " Constraint: " + self.constraint.get_name()
        self.name += ", Number of assertions: " + str(self.number_of_invs) + "\n"
        self.name += "".join([child.__repr__() for child in self.children])
        return self.name

    def collect_all_child_assertions(self):
        # this function "flattens" the decision tree, collects all invariants in all nodes,
        # and stores them as a list in self.constrained_invariants.

        if self.node_assertion is not None and self.node_assertion.number_of_invs > 0:
            self.number_of_invs += self.node_assertion.number_of_invs
            self.constrained_invariants.append(
                ConstrainedInv(
                    constraint=self.constraint,
                    data_assertion=self.node_assertion,
                    id=len(self.constrained_invariants),
                )
            )
        for child in self.children:
            for c_assertion in child.constrained_invariants:
                updated_constraint = ConjunctiveConstraint(
                    self.constraint.single_constraints
                    + c_assertion.constraint.single_constraints
                )
                updated_constrained_invariant = ConstrainedInv(
                    constraint=updated_constraint,
                    data_assertion=c_assertion.data_assertion,
                    id=len(self.constrained_invariants),
                )
                self.constrained_invariants.append(updated_constrained_invariant)
            self.number_of_invs += child.number_of_invs


# ---------------------------------------------------------------
# Class DecisionTreeAssertion: A tree containing ConstrainedInvs at its nodes.
# ---------------------------------------------------------------
class DecisionTreeAssertion(Assertion):
    @staticmethod
    def learn(df, **kwargs):
        return _DecisionTreeAssertionBuilderImpl(df=df, **kwargs).learn()

    def __init__(self, df, features, root):
        super().__init__(_AssertionType.DECISION_TREE_ASSERTION, df, features)
        self.root = root
        self.populate(self.root.constrained_invariants)

    def get_level_wise_explanation(self, df, result):
        df = df.dropna()

        if df.empty:
            return None

        if self.is_valid():
            explanation = self.compute_explanation(
                self.root, df, result._row_wise_inv_violation
            )
            explanation.process(fraction_violation=1.0)

            # A tree-view in json, which explains  under what constraint how much violation is observed
            return explanation.get_details()

        return None

    def compute_explanation(self, node, df, row_wise_inv_violation):
        compatible_rows = node.constraint.apply(df).index.values.astype(int)
        valid = True
        if len(compatible_rows) == 0:
            valid = False
        cur_row_wise_inv_violation = row_wise_inv_violation.loc[
            compatible_rows, node.starting_invariant_id :
        ]
        cur_row_wise_violation = pd.DataFrame(columns=["violation"])
        cur_row_wise_violation["violation"] = np.max(cur_row_wise_inv_violation, axis=1)
        cur_row_wise_violation = cur_row_wise_violation.fillna(0)
        cur_violation = np.sum(cur_row_wise_violation, axis=0)["violation"]
        explanation = ViolationExplanation(
            constraint=node.constraint,
            violation=cur_violation,
            nRows=len(compatible_rows),
            valid=valid,
        )

        for child in node.children:
            cur_df = node.constraint.apply(df)
            child_explanation = self.compute_explanation(
                child, cur_df, row_wise_inv_violation
            )
            if child_explanation.valid:
                explanation.add_child(child_explanation)
        return explanation


# ---------------------------------------------------------------
# Class _DecisionTreeAssertionBuilderImpl
# ---------------------------------------------------------------
class _DecisionTreeAssertionBuilderImpl:
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
        max_tree_depth,
        max_self_violation,
        cross_validate,
        n_fold,
        assertion_improvement_factor,
    ):
        class Parameters:
            def __init__(
                self,
                max_col_in_slice,
                slice_col_overlap,
                max_row_in_slice,
                use_const_term,
                standardize_pca,
                max_unique_value_per_categorical_attribute,
                max_allowed_number_of_constraint_per_attribute,
                partition_on_categorical_attribute_only,
                max_tree_depth,
                max_self_violation,
                cross_validate,
                n_fold,
                assertion_improvement_factor,
            ):
                self.max_col_in_slice = max_col_in_slice
                self.slice_col_overlap = slice_col_overlap
                self.max_row_in_slice = max_row_in_slice
                self.use_const_term = use_const_term
                self.standardize_pca = standardize_pca
                self.max_unique_value_per_categorical_attribute = (
                    max_unique_value_per_categorical_attribute
                )
                self.max_allowed_number_of_constraint_per_attribute = (
                    max_allowed_number_of_constraint_per_attribute
                )
                self.partition_on_categorical_attribute_only = (
                    partition_on_categorical_attribute_only
                )
                self.max_tree_depth = max_tree_depth
                self.max_self_violation = max_self_violation
                self.cross_validate = cross_validate
                self.n_fold = n_fold
                self.assertion_improvement_factor = assertion_improvement_factor

        self._input_df = df
        self.parameters = Parameters(
            max_col_in_slice,
            slice_col_overlap,
            max_row_in_slice,
            use_const_term,
            standardize_pca,
            max_unique_value_per_categorical_attribute,
            max_allowed_number_of_constraint_per_attribute,
            partition_on_categorical_attribute_only,
            max_tree_depth,
            max_self_violation,
            cross_validate,
            n_fold,
            assertion_improvement_factor,
        )

    def learn(self):
        if self.parameters.max_tree_depth > 1:
            assert (
                self.parameters.partition_on_categorical_attribute_only == True
            ), "Currently decision tree assertion only supports splitting on categorical attributes only."

        return DecisionTreeAssertion(
            df=self._input_df,
            features=list(self._input_df.columns),
            # constructor of _Node class is recursive. It builds a decision tree assertion,
            # and returns the root of the decision tree.
            root=_Node(
                df=self._input_df,
                node_depth=1,
                constraint=ConjunctiveConstraint([]),
                parameters=self.parameters,
            ),
        )

    def __repr__(self):
        return """DecisionTreeAssertionBuilder
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
    partition on categorical attribute_only = {8}
    max tree depth = {9}
    max self violation = {10}
    cross validate = {11}
    number of folds = {12}
    children assertion improvement factor = {13}""".format(
            self._input_df,
            self.parameters.max_col_in_slice,
            self.parameters.max_row_in_slice,
            self.parameters.slice_col_overlap,
            self.parameters.use_const_term,
            self.parameters.standardize_pca,
            self.parameters.max_unique_value_per_categorical_attribute,
            self.parameters.max_allowed_number_of_constraint_per_attribute,
            self.parameters.partition_on_categorical_attribute_only,
            self.parameters.max_tree_depth,
            self.parameters.max_self_violation,
            self.parameters.cross_validate,
            self.parameters.n_fold,
            self.parameters.assertion_improvement_factor,
        )
