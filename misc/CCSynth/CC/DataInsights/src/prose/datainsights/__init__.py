# Copyright (c) Microsoft Corporation.  All rights reserved.

"""
This package contains the PROSE Data Insights SDK. It is a runtime package which 
assists with gaining insights about your data.
"""

from ._api._learn_assertions import learn_assertions, DataAssertion

from ._version import __version__

__all__ = [
    "learn_assertions",
    "DataAssertion",
    "sample",
    "profile",
    "DataDiversitySample",
    "DataProfile",
    "DataMode",
    "ProfileTypes",
]
