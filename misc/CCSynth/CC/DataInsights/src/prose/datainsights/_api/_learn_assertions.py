# Copyright (c) Microsoft Corporation.  All rights reserved.
from .._learn_assertions import learn_assertions_impl


def learn_assertions(df, **kwargs):
    """Learn assertions on the given dataframe.

    :param df: The dataframe whose assertions are to be learnt.
    :type df: Must be a pandas dataframe.
    :key max_col_in_slice: Maximum number of columns to use when learning any single assertion (default 40).
    :type max_col_in_slice: integer
    :key slice_col_overlap: The amount by which columns should overlap when slicing the given dataframe into max_col_in_slice columns.
    :type slice_col_overlap: integer
    :key max_row_in_slice: Number of rows to use in each slice when learning assertions (default 10000). Irrespective of this parameter, the returned learnt assertions will apply to the whole dataframe.
    :type max_row_in_slice: integer
    :key use_const_term: Flag indicating if a constant term should be learnt in the invariants (default True); that is, learn invariants of the form w_1x_1 + ...+ w_nx_n + b = 0, where b != 0 if the flag is True
    :type use_const_term: bool
    :key: standardize_pca: Whether to standardize the data by subtracting mean and dividing by standard deviation before performing PCA.
    :type: standardize_pca: bool
    :key learn_disjunctive: Boolean flag indicating if disjunctive assertions should be learnt (default False).
    :type learn_disjunctive: bool
    :key learn_decision_tree: Boolean flag indicating if decision tree assertions should be learnt (default False).
    :type learn_decision_tree: bool
    :key max_unique_value_per_categorical_attribute: The maximum number of unique values in an attribute to trigger learning of disjunctive assertions (default 50).
    :type max_unique_value_per_categorical_attribute: integer or None. None implies no bound.
    :key max_allowed_number_of_constraint_per_attribute: The maximum number of disjunctions to learn per attribute (default None).
    :type max_allowed_number_of_constraint_per_attribute: integer or None. None implies no bound.
    :key partition_on_categorical_attribute_only: Whether to consider only categorical attributes for learning disjunction (default True).
    :type partition_on_categorical_attribute_only: Boolean.
    :key max_tree_depth: Maximum depth of the decision tree (default 10).
    :type max_tree_depth: integer
    :key max_self_violation: Maximum self violation allowed for an outlier.
    :type max_self_violation: real number within the range [0, 1]
    :key cross_validate: Whether to cross validate or not.
    :type cross_validate: bool.
    :key n_fold: Number of folds to use for cross-validation.
    :type n_fold: Integer >= 1
    :key assertion_improvement_factor: Factor by which assertion quality must improve to consider further splitting.
    :type assertion_improvement_factor: real value between [0, 1]
    :returns: A result object that contains the learnt assertions.
    :rtype: :class:`prose.datainsights.DataAssertions` """

    defaults = {
        "max_col_in_slice": 40,
        "slice_col_overlap": 10,
        "max_row_in_slice": 10000,
        "use_const_term": True,
        "standardize_pca": False,
        "learn_disjunctive": False,
        "learn_decision_tree": False,
        "max_unique_value_per_categorical_attribute": 50,
        "max_allowed_number_of_constraint_per_attribute": None,
        "partition_on_categorical_attribute_only": True,
        "max_tree_depth": 5,
        "max_self_violation": 1e-6,
        "cross_validate": True,
        "n_fold": 5,
        "assertion_improvement_factor": 0.8,
    }
    defaults.update(kwargs)
    assertion_implementation = learn_assertions_impl(df, **defaults)
    return DataAssertion(assertion_implementation)


class DataAssertion:
    """The result from learning assertions for a given data set.

    (The result of calling :meth:`prose.datainsights.learn_assertions`.) """

    def __init__(self, result):
        """Only intended to be called internally."""
        self._impl = result

    def evaluate(self, df, **kwargs):
        """ Monitor the given df against the pre-computed invariants.

        :param df: The dataframe to monitor. This should have the same shape as the dataframe used to 
        learn this assertion.
        :key sample_n: The number of rows to sample when evaluating this dataframe (default 20000). Evaluation of the assertion is over complete df if sample_n is set to None. 
        :type sample_n: int or None
        :key random_state: The seed to use to perform the random sampling of sample_n rows (default 1).
        :type random_state: int
        :key early_termination: Flag indicating if evaluation should terminate early if violation is detected.
        :type early_termination: bool
        :key early_threshold: Threshold on the degree of assertion violation to use when classifying a row as a violating row (for early termination) (default 0.75)
        :type early_threshold: float in range [0,1]
        :key early_percentage: The (min) fraction of violating rows that triggers early termination (default 0.1).
        :type early_percentage: float in range [0,1]
        :key ignore_additional_columns: If true, ignore columns present in the test dataset that were not present in the training dataset (default False).
        :type ignore_additional_columns: bool
        :key explanation: Whether to return detailed explanation or not
        :type explanation: boolean
        :return: A dictionary containing monitoring result. The keys are 'worst_violation' (measure of the violation
        measure for the worst row), 'violation_name' (the name of the invariant being violated by the worst row),
        'worst_row' (the index of the worst row), and 'avg_violation' (average violation across all (sampled) rows).
        'fraction_of_rows_tested', (fraction of data rows that were checked against at least one invariant)."""

        return self._impl.evaluate(df, kwargs)

    def size(self):
        """Return the number of sub-assertions represented in this object"""
        return self._impl.size()

    def __repr__(self):
        """Return a printable string that summarizes the assertions stored in this object."""
        return self._impl.__repr__()
