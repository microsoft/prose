# Copyright (c) Microsoft Corporation.  All rights reserved.

from setuptools import setup, find_packages
from distutils.util import convert_path

globals = {}
ver_path = convert_path("src/prose/datainsights/_version.py")
with open(ver_path) as ver_file:
    exec(ver_file.read(), globals)

with open("README.md", "r") as fh:
    long_description = fh.read()

if __name__ == "__main__":
    setup(
        name="prose-datainsights",
        description="",
        license="LICENSE.txt",
        url="https://microsoft.github.io/prose",
        version=globals["__version__"],
        author="Microsoft Corporation",
        author_email="prose-contact@microsoft.com",
        keywords=["data", "analysis"],
        long_description=long_description,
        packages=find_packages(where="src"),
        package_dir={"": "src"},
        classifiers=[
            "Development Status :: 3 - Alpha",
            "Intended Audience :: Developers",
            "Intended Audience :: Science/Research",
            "License :: Other/Proprietary License",
            "Operating System :: Microsoft :: Windows",
            "Operating System :: MacOS",
            "Operating System :: POSIX :: Linux",
            "Programming Language :: Python",
            "Programming Language :: Python :: 3.5",
            "Programming Language :: Python :: 3.6",
            "Programming Language :: Python :: 3.7",
            "Programming Language :: Python :: 3.8",
            "Programming Language :: Python :: 3.9",
            "Programming Language :: Python :: Implementation :: CPython",
            "Topic :: Software Development",
            "Private :: Do Not Upload",
        ],
        namespace_packages=["prose"],
        include_package_data=True,
        install_requires=[
            "Jinja2==2.10.1",
            "pandas >= 0.24.2",
            "protobuf >= 3.8.0",
            "regex >= 2017.7.28",
            "scipy >= 1.2.1",
        ],
        zip_safe=False,
        data_files=[],
    )
