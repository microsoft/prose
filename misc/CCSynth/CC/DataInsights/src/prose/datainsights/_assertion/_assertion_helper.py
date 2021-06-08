# Copyright (c) Microsoft Corporation.  All rights reserved.
import numpy as np
import pandas as pd
import copy

"""
    This file contains miscellaneous helper classes and functions for the data-assertion routines
"""


# ---------------------------------------------------------------
# Class _AssertionType
# ---------------------------------------------------------------
class _AssertionType:
    PCA_ASSERTION = "PCA Assertion"
    DISJUNCTIVE_ASSERTION = "Disjunctive Assertion"
    DECISION_TREE_ASSERTION = "Decision Tree Assertion"
    MIXED = "Mixed Assertion"


# ---------------------------------------------------------------
# Class _RelationalOperators: Static class for various relational operators
# ---------------------------------------------------------------
class _RelationalOperators:
    EQUAL = 1
    LESS_THAN = 2
    LESS_THAN_EQUAL_TO = 3
    GREATER_THAN = 4
    GREATER_THAN_EQUAL_TO = 5
    NOT_EQUAL = 6
    INCLUSIVE_RANGE = 7
    EXCLUSIVE_RANGE = 8

    OPERATOR_STRING = {
        EQUAL: "=",
        LESS_THAN: "<",
        LESS_THAN_EQUAL_TO: "<=",
        GREATER_THAN: ">",
        GREATER_THAN_EQUAL_TO: ">=",
        NOT_EQUAL: "!=",
        INCLUSIVE_RANGE: "in=",
        EXCLUSIVE_RANGE: "in",
    }


# ---------------------------------------------------------------
# Class SingleConstraint
#         A single constraint requires
#         (1) column_name: name of a column of the data frame.
#         (2) column_value: values of that column that defines the constraint.
#         This is either a single value or a two elements list representing a range.
#         (3) relational_op: defines the relation between column_name and column_value.
#         Example:
#             Constraint "make = Toyota":
#                 column_name = 'make',
#                 column_value = 'Toyota',
#                 relational_op = _RelationalOperators.EQUAL
#
#             Constraint: "30 <= mpg <= 35"
#                 column_name = 'mpg',
#                 column_value = [30, 35],
#                 relational_op = _RelationalOperators.INCLUSIVE_RANGE
# ---------------------------------------------------------------
class SingleConstraint:
    def __init__(self, column_name, column_value, relational_op):
        self.column_name = column_name
        self.column_value = column_value
        self.relational_op = relational_op

        # if the relational_op requires a range, then column_value must be of type list
        # for all other relational operators, column_value must not be of type list
        assert isinstance(self.column_value, list) == (
            self.relational_op
            in [
                _RelationalOperators.INCLUSIVE_RANGE,
                _RelationalOperators.EXCLUSIVE_RANGE,
            ]
        ) and (
            not isinstance(self.column_name, list) or len(self.column_value) == 2
        ), "Invalid operator and value combination"
        assert self.relational_op is not None, "Operator not supported"

    def apply(self, df, drop_column=True):
        """ Returns the transformed dataframe after the constraint is applied
        :param df: source dataframe
        :param drop_column: boolean flag. denotes whether to drop the column, on which constraint is applied, or not
        :return: df with the constraint applied
        """
        new_df = df
        if self.relational_op == _RelationalOperators.EQUAL:
            new_df = df[df[self.column_name] == self.column_value]
        elif self.relational_op == _RelationalOperators.LESS_THAN:
            new_df = df[df[self.column_name] < self.column_value]
        elif self.relational_op == _RelationalOperators.LESS_THAN_EQUAL_TO:
            new_df = df[df[self.column_name] <= self.column_value]
        elif self.relational_op == _RelationalOperators.GREATER_THAN:
            new_df = df[df[self.column_name] > self.column_value]
        elif self.relational_op == _RelationalOperators.GREATER_THAN_EQUAL_TO:
            new_df = df[df[self.column_name] >= self.column_value]
        elif self.relational_op == _RelationalOperators.NOT_EQUAL:
            new_df = df[df[self.column_name] != self.column_value]
        elif self.relational_op == _RelationalOperators.INCLUSIVE_RANGE:
            new_df = df[
                (df[self.column_name] >= self.column_value[0])
                & (df[self.column_name] <= self.column_value[1])
            ]
        elif self.relational_op == _RelationalOperators.EXCLUSIVE_RANGE:
            new_df = df[
                (df[self.column_name] > self.column_value[0])
                & (df[self.column_name] < self.column_value[1])
            ]
        if drop_column:
            return new_df.loc[:, new_df.columns != self.column_name]
        else:
            return new_df

    def get_name(self):
        return repr(self)

    def __repr__(self):
        if isinstance(self.column_value, list):
            return (
                str(self.column_value[0])
                + _RelationalOperators.OPERATOR_STRING[self.relational_op]
                + str(self.column_name)
                + _RelationalOperators.OPERATOR_STRING[self.relational_op]
                + str(self.column_value[1])
            )
        else:
            return (
                str(self.column_name)
                + _RelationalOperators.OPERATOR_STRING[self.relational_op]
                + str(self.column_value)
            )


# ---------------------------------------------------------------
# Class DisjunctiveConstraint: A disjunctive constraint is disjunction of multiple _SingleConstraints
# ---------------------------------------------------------------
class DisjunctiveConstraint:
    def __init__(self, single_constraints):
        self.single_constraints = single_constraints
        self.column_name = single_constraints[0].column_name

        # There should be at most one column involved in a disjunctive constraint for now
        assert (
            len(set(constraint.column_name for constraint in single_constraints)) == 1
        )

    def apply(self, df, drop_column=True):
        """ Returns the transformed dataframe after the constraint is applied
        :param df: the source data frame
        :param drop_column: boolean flag. denotes whether to drop the column, on which constraint is applied, or not
        :return: new_df: with the constraint applied
        """
        partitioned_dfs = []
        for constraint in self.single_constraints:
            partitioned_dfs.append(constraint.apply(df, drop_column))

        new_df = pd.concat(partitioned_dfs)
        return new_df

    def get_name(self):
        return self.__repr__()

    def __repr__(self):
        return " || ".join(["{0}".format(c) for c in self.single_constraints])


# ---------------------------------------------------------------
# Class ConjunctiveConstraint: A conjunctive constraint is conjunction of multiple _SingleConstraints
# ---------------------------------------------------------------
class ConjunctiveConstraint:
    def __init__(self, single_constraints):
        self.single_constraints = single_constraints

        # Each constraint within conjunctive constraint should be on different columns
        assert len(
            set(constraint.column_name for constraint in single_constraints)
        ) == len(single_constraints)

    def apply(self, df, drop_column=True):
        """ Returns the transformed dataframe after the constraint is applied
        :param df: the source data frame
        :param drop_column: boolean flag. denotes whether to drop the column, on which constraint is applied, or not
        :return: new_df: with the constraint applied
        """
        new_df = df
        for constraint in self.single_constraints:
            new_df = constraint.apply(new_df, drop_column)
        return new_df

    def get_name(self):
        return self.__repr__()

    def __repr__(self):
        if len(self.single_constraints) == 0:
            return "None"
        return " && ".join(["{0}".format(c) for c in self.single_constraints])


# ---------------------------------------------------------------
# Class ConstrainedInv
# A constrained invariant consists of
#         -- constraint: the constraint of the invariant
#         -- data_assertion: a DataAssertion object which encodes invariant(s)
#            that only apply on rows where the constraint holds
# ---------------------------------------------------------------
class ConstrainedInv:
    def __init__(self, constraint, data_assertion, id=None):
        self.id = id
        self.constraint = constraint
        self.data_assertion = data_assertion
        self._valid = data_assertion is not None and data_assertion.is_valid()

    def is_valid(self):
        return self._valid

    def evaluate(self, df, options, apply_constraint=True):

        df_to_evaluate = df

        if apply_constraint:
            df_to_evaluate = self.constraint.apply(df)

        result = self.data_assertion.evaluate(df_to_evaluate, options)

        if df.shape[0] > 0:
            result.fraction_of_rows_tested *= (
                float(df_to_evaluate.shape[0]) / df.shape[0]
            )
        return result

    def _get_name(self):
        return self.__repr__()

    def __repr__(self):
        return (
            "Constraint: "
            + self.constraint.get_name()
            + " --> "
            + "Number of assertions: "
            + str(self.data_assertion.number_of_invs)
            + ", Detailed assertions: "
            + " && ".join(
                [
                    self.data_assertion._get_name(i)
                    for i in range(self.data_assertion.number_of_invs)
                ]
            )
        )


# ---------------------------------------------------------------
# Class ViolationResult: class for storing violation results when checked against assertions
# ---------------------------------------------------------------
class ViolationResult:
    def __init__(
        self,
        train_df=None,
        test_df=None,
        assertions=None,
        worst_violation=0.0,
        violation_name=None,
        worst_row=None,
        avg_violation=0.0,
        fraction_of_rows_tested=0.0,
        level_wise_violation=None,
        row_wise_violation_summary=None,
        row_wise_per_attribute_violation_contribution=None,
        compute_explanation=False,
    ):
        self.train_df = train_df
        self.test_df = test_df
        self.assertions = assertions
        self.worst_violation = worst_violation
        self.violation_name = violation_name
        self.worst_row = worst_row
        self.avg_violation = avg_violation
        self.fraction_of_rows_tested = fraction_of_rows_tested
        self.level_wise_violation = level_wise_violation
        self.row_wise_violation_summary = row_wise_violation_summary
        self.row_wise_per_attribute_violation_contribution = (
            row_wise_per_attribute_violation_contribution
        )
        self.compute_explanation = compute_explanation

        self._row_wise_inv_violation = None
        self._row_wise_inv_compatibility = None
        self._baseline = None

    def _heatmap_highlighter(self, s, reference=None):
        a = reference.loc[s.name, :].copy()
        rng = max(1, float(max(a) - min(a)))
        colors = 229 - a * 229 / rng
        colors = [
            "#ff" + str(hex(int(c)))[2:].zfill(2) + str(hex(int(c)))[2:].zfill(2)
            for c in colors
        ]
        return ["background-color: %s" % color for color in colors]

    def _get_sampled_indexes_and_baseline_df(self, violation_threshold, sample_only=True):
        sampled_indexes = []
        baseline_df = None

        for k in reversed(self.assertions.constrained_invariants):
            cur_df = pd.DataFrame(
                self.row_wise_violation_summary.loc[
                    list(k.constraint.apply(self.test_df).index)
                ]["violation"]
            ).sort_values(by=["violation"], ascending=False)

            valid = False
            if cur_df.empty:
                continue

            if sample_only:
                # Pick a random sample as a representative row from each decision tree partition
                for idx in list(
                    cur_df[cur_df["violation"] == max(cur_df["violation"])]
                    .sample(frac=1)
                    .index
                ):
                    if cur_df.loc[idx]["violation"] < violation_threshold:
                        break
                    if idx not in sampled_indexes:
                        sampled_indexes.append(idx)
                        valid = True
                        break

                if not valid:
                    continue
            else:
                newly_added = 0
                for i in cur_df.index:
                    if i not in sampled_indexes:
                        sampled_indexes.append(i)
                        newly_added += 1


            cur_train_df_numeric = k.constraint.apply(self.train_df)._get_numeric_data()
            cur_train_df_categorical = k.constraint.apply(
                self.train_df, drop_column=False
            )[
                [
                    col
                    for col in self.train_df.columns
                    if col not in cur_train_df_numeric.columns
                ]
            ]

            # Compute a baseline dataframe to visually contrast with the representative violating rows
            current_mean_row = pd.DataFrame(
                np.array(cur_train_df_numeric.mean()).reshape(
                    (1, len(cur_train_df_numeric.columns))
                ),
                columns=cur_train_df_numeric.columns,
            )

            if len(cur_train_df_categorical.columns) > 0:
                current_mode_row = pd.DataFrame(
                    np.array(cur_train_df_categorical.mode()).reshape(
                        (-1, len(cur_train_df_categorical.columns))
                    ),
                    columns=cur_train_df_categorical.columns,
                )[:1]

            cur_row = pd.DataFrame(columns=self.train_df.columns)

            for col in self.train_df.columns:
                if col in cur_train_df_numeric.columns:
                    cur_row[col] = current_mean_row[col]
                else:
                    cur_row[col] = current_mode_row[col]

            if not sample_only:
                cur_row = pd.DataFrame(np.tile(np.array(cur_row), (newly_added, 1)),
                                       columns=self.train_df.columns)
                for col in self.train_df.columns:
                    if col in cur_train_df_numeric.columns:
                        cur_row[col] = cur_row[col].apply(float)
            if baseline_df is None:
                baseline_df = cur_row
            else:
                baseline_df = pd.concat([baseline_df, cur_row], ignore_index=True)

        return sampled_indexes, baseline_df

    def get_most_violating_indices(self, num_most_violating_indices):
        """ Return the indices of the num_most_violating_indices most violating rows.
        The indices are positions, not locations, i.e., they are to be used with DataFrame.iloc instead
        of `DataFrame.loc`."""

        if self.test_df.empty:
            return []

        num_most_violating_indices = min(
            num_most_violating_indices, self.row_wise_violation_summary.shape[0]
        )
        violations = self.row_wise_violation_summary.loc[self.test_df.index][
            "violation"
        ]
        indices = np.argpartition(violations, -num_most_violating_indices)[
            -num_most_violating_indices:
        ]
        return indices

    def preview(self, violation_threshold=0.0, sample_only=True):
        if not self.compute_explanation:
            return "Explanation not requested while evaluation. Try evaluation with explanation=True."
        if self.test_df.empty:
            return "Cannot generate preview when test data frame is empty."

        # Now compute responsibility of each attribute within the representative sample rows
        self.train_df = self.train_df.dropna()
        self.test_df = self.test_df.dropna()
        sampled_indexes, baseline = self._get_sampled_indexes_and_baseline_df(
            violation_threshold,
            sample_only,
        )
        if len(sampled_indexes) == 0 or baseline is None:
            return "No violation to preview with violation threshold: " + str(
                violation_threshold
            )

        numeric_columns = baseline._get_numeric_data().columns

        expected = np.array(baseline._get_numeric_data())
        found = np.array(
            self.row_wise_violation_summary.loc[sampled_indexes][
                baseline.columns
            ]._get_numeric_data()
        )

        violation_amount = pd.DataFrame(
            np.abs(found - expected), columns=numeric_columns, index=sampled_indexes
        )
        coeffs = self.row_wise_per_attribute_violation_contribution.loc[
            sampled_indexes
        ][numeric_columns]
        self.reference = np.multiply(
            violation_amount, coeffs
        )  # Violation magnitude is multiplied by PCA co-effs. This works as an auto-normalization

        _violation_heatmap = self.row_wise_violation_summary.loc[
            sampled_indexes
        ].style.apply(
            self._heatmap_highlighter,
            reference=self.reference,
            subset=numeric_columns,
            axis=1,
        )
        self._baseline = baseline
        return _violation_heatmap

    def get_level_wise_violation_in_json(self):
        return self.level_wise_violation

    def get_baseline(self):
        return self._baseline

    def get_assertions(self):
        return self.assertions

    def get_values_in_dict(self):
        # Returning summarized values only, in a dict. For backward compatibility.
        return {
            "worst_violation": str(self.worst_violation),
            "violation_name": str(self.violation_name),
            "worst_row": str(self.worst_row),
            "avg_violation": str(self.avg_violation),
            "fraction_of_rows_tested": str(self.fraction_of_rows_tested),
        }


# ----------------------------------------------------------------------------
# Class Assertion: an interface which other assertions should implement
# ----------------------------------------------------------------------------
class Assertion:
    @staticmethod
    def learn(df, **kwargs):
        return "Not implemented"

    def __init__(self, type, df, features):
        self.type = type
        self.input_df = df
        self.features = features
        self.number_of_invs = 0
        self._valid = False

    def populate(self, constrained_invariants):
        self.constrained_invariants = constrained_invariants

        self.number_of_invs = sum(
            [inv.data_assertion.number_of_invs for inv in self.constrained_invariants]
        )
        self._valid = self.number_of_invs > 0
        if not self._valid:
            return

        self.inv_matrix = np.concatenate(
            [c_inv.data_assertion.inv_matrix for c_inv in self.constrained_invariants],
            axis=1,
        )
        self.inv_names = []
        for c_inv in self.constrained_invariants:
            self.inv_names.extend(
                [
                    "<"
                    + repr(c_inv.constraint)
                    + "> "
                    + c_inv.data_assertion._get_name(i)
                    for i in range(c_inv.data_assertion.size())
                ]
            )

        self.mean = np.concatenate(
            [c_inv.data_assertion.mean for c_inv in self.constrained_invariants]
        )
        self.min = np.concatenate(
            [c_inv.data_assertion.mins for c_inv in self.constrained_invariants]
        )
        self.max = np.concatenate(
            [c_inv.data_assertion.maxs for c_inv in self.constrained_invariants]
        )
        self.std_dev = np.concatenate(
            [c_inv.data_assertion.std_dev for c_inv in self.constrained_invariants]
        )
        self.std_dev_all = np.concatenate(
            [c_inv.data_assertion.std_dev_all for c_inv in self.constrained_invariants]
        )
        self.std_dev_inv = np.concatenate(
            [c_inv.data_assertion.std_dev_inv for c_inv in self.constrained_invariants]
        )

    def evaluate(self, df, monitoring_options):
        df = df.dropna()

        result = ViolationResult(
            train_df=self.input_df,
            test_df=df,
            assertions=self,
            fraction_of_rows_tested=1.0,
        )
        if df.empty:
            return result

        if monitoring_options.get("ignore_additional_columns", False):
            df = df[self.features]

        explanation = monitoring_options.get("explanation", False)
        normalizeViolation = monitoring_options.get("normalizeViolation", True)

        if self.is_valid():
            result._row_wise_inv_violation, result._row_wise_inv_compatibility, result.row_wise_violation_summary, result.row_wise_per_attribute_violation_contribution = self.validate(
                df, explanation=explanation, normalizeViolation=normalizeViolation
            )

            number_of_rows_tested = np.count_nonzero(
                result.row_wise_violation_summary["num_of_invs"], axis=0
            )

            # This is the highest level summary of the violation
            # for each row, violation is computed as a weighted average of violation
            # over all compatible invariants. We take the worst of them and report
            # as worst_violation.
            result.worst_violation = np.amax(
                result.row_wise_violation_summary["violation"]
            )
            result.worst_row = result.row_wise_violation_summary["violation"].idxmax()
            # since we take a weighted average, we include all compatible invariants in the violation_name
            worst_row_compatibility = result._row_wise_inv_compatibility.loc[
                result.worst_row, :
            ]
            # There may be many rows that have the same index as worst_row, we pick the first
            if type(worst_row_compatibility) is pd.DataFrame:
                worst_row_compatibility = worst_row_compatibility.iloc[0]
            result.violation_name = " && ".join(
                inv_name
                for i, inv_name in enumerate(self.inv_names)
                if worst_row_compatibility.iloc[i] == 1
            )
            result.avg_violation = np.sum(
                result.row_wise_violation_summary["violation"], axis=0
            ) / float(max(number_of_rows_tested, 1))
            result.fraction_of_rows_tested = number_of_rows_tested / float(df.shape[0])

        return result

    def validate(self, df, explanation, normalizeViolation=True):
        # N = number of data points, I = number of invs, C = number of cols
        # Returns
        #   row_wise_inv_violation, (N X I)
        #       : violation for each invariant by each row
        #   row_wise_inv_compatibility,  (N X I)
        #       : a boolean mask representing which invariant is compatible with which row
        #   row_wise_violation_summary, (N X 2) or (N X C+2)
        #       : aggregated violation for each row, over all invariants
        #       : when explanation is not required, returns 2 new columns for each row --- "violation", "num_of_invs"
        #       : when explanation is required, appends the above 2 new columns to the original dataframe
        #   row_wise_per_attribute_violation_contribution (N X C+2)
        #       : assigns blame to each attribute for violation

        def distance_to_violation_degree(d):
            d = np.where(d > 0, d, 0)
            d = np.multiply(d, self.std_dev_inv)
            if normalizeViolation:
                d = 1 - np.exp(-d)
            return d

        cur_df = self.constrained_invariants[0].data_assertion._transform(df).dropna()
        N, C, I = cur_df.shape[0], cur_df.shape[1], self.inv_matrix.shape[1]

        row_wise_inv_violation = pd.DataFrame(np.zeros((N, I)), index=df.index)
        row_wise_inv_compatibility = pd.DataFrame(np.zeros((N, I)), index=df.index)
        row_wise_violation_summary = pd.DataFrame(
            columns=["violation", "num_of_invs"], index=df.index
        )
        row_wise_per_attribute_violation_contribution = pd.DataFrame(
            np.zeros((N, C)), index=cur_df.index, columns=cur_df.columns
        )

        ######### Computing row_wise_inv_compatibility #########
        inv_count = 0
        for i in range(len(self.constrained_invariants)):
            compatible_rows = (
                self.constrained_invariants[i].constraint.apply(df).index.values
            )
            cur_inv_count = self.constrained_invariants[i].data_assertion.size()
            row_wise_inv_compatibility.loc[
                compatible_rows, inv_count : inv_count + cur_inv_count - 1
            ] = 1
            inv_count += cur_inv_count
        ########################################################

        ########### Computing row_wise_inv_violation ###########
        row_wise_inv_weight = np.multiply(
            row_wise_inv_compatibility, 1 / np.log(2 + self.std_dev)
        )
        row_wise_inv_weight = np.multiply(
            row_wise_inv_weight.T,
            1
            / np.maximum(
                np.full((row_wise_inv_weight.shape[0],), 1e-10),
                np.sum(row_wise_inv_weight, axis=1),
            ),
        ).T
        s = np.dot(cur_df, self.inv_matrix)
        v1 = distance_to_violation_degree(np.abs(s - self.mean) - 4 * self.std_dev)
        v2 = distance_to_violation_degree(self.min - s)
        v3 = distance_to_violation_degree(s - self.max)
        violations = np.maximum(np.maximum(v1, v2), v3)

        row_wise_inv_violation = pd.DataFrame(
            np.multiply(
                np.multiply(violations, row_wise_inv_compatibility), row_wise_inv_weight
            ),
            index=df.index,
        )
        ########################################################

        ########### Computing row_wise_violation_summary ###########
        row_wise_violation_summary["num_of_invs"] = np.sum(
            row_wise_inv_compatibility, axis=1
        )
        row_wise_violation_summary["violation"] = np.sum(row_wise_inv_violation, axis=1)
        row_wise_violation_summary = row_wise_violation_summary.fillna(0)
        ############################################################

        if explanation:
            # Only compute row_wise_per_attribute_violation_contribution when explanation is required
            # Also, update row_wise_violation_summary to include the original dataframe for ease of exposition
            row_wise_violation_summary, row_wise_per_attribute_violation_contribution = self.update_for_explanation(
                df, row_wise_inv_violation, row_wise_violation_summary
            )

        return (
            row_wise_inv_violation,
            row_wise_inv_compatibility,
            row_wise_violation_summary,
            row_wise_per_attribute_violation_contribution,
        )

    def update_for_explanation(
        self, df, row_wise_inv_violation, row_wise_violation_summary
    ):
        cur_df = self.constrained_invariants[0].data_assertion._transform(df).dropna()
        inv_matrix_T = self.inv_matrix.T
        column_names = list(cur_df.columns)

        if "_one" in column_names:
            inv_matrix_T = inv_matrix_T[:, :-1]
            column_names.remove("_one")

        for col in self.features:
            row_wise_violation_summary[col] = df[col]

        row_wise_per_attribute_violation_contribution = np.abs(
            np.dot(row_wise_inv_violation, inv_matrix_T)
        )
        row_wise_per_attribute_violation_contribution = (
            row_wise_per_attribute_violation_contribution
            / (
                np.linalg.norm(
                    row_wise_per_attribute_violation_contribution, axis=1
                ).reshape(cur_df.shape[0], 1)
                + 1e-16
            )
        )
        row_wise_per_attribute_violation_contribution = pd.DataFrame(
            row_wise_per_attribute_violation_contribution,
            columns=column_names,
            index=cur_df.index,
        )
        row_wise_per_attribute_violation_contribution["violation"] = np.array(
            row_wise_violation_summary["violation"]
        )
        row_wise_per_attribute_violation_contribution["num_of_invs"] = np.array(
            row_wise_violation_summary["num_of_invs"]
        )
        row_wise_per_attribute_violation_contribution.sort_values(
            by=["violation", "num_of_invs"],
            kind="mergesort",
            ascending=False,
            inplace=True,
        )

        row_wise_violation_summary.sort_values(
            by=["violation", "num_of_invs"],
            kind="mergesort",
            ascending=False,
            inplace=True,
        )
        return row_wise_violation_summary, row_wise_per_attribute_violation_contribution

    def size(self):
        return len(self.constrained_invariants)

    def get_inv_count(self):
        return self.number_of_invs

    def code(self):
        raise NotImplementedError(
            "Code generation is not currently supported for this result object. "
            "Use monitor to run this assertion check on a new dataframe."
        )

    def is_valid(self):
        return self._valid

    def __repr__(self):
        if self.number_of_invs > 0:
            return (
                self.type
                + ":\n"
                + "\n".join(["\t\t{0}".format(i) for i in self.constrained_invariants])
            )
        else:
            return self.type + ":\n" + "\t\tNone"
