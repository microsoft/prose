from setuptools import setup, find_packages

setup(
    name="swe_sharp_bench",
    version="0.1.0",
    description="C# extension for SWE-bench evaluation harness",
    author="Sanket Mhatre",
    author_email="t-smhatre@microsoft.com",
    packages=find_packages(),
    install_requires=[
        "swebench==3.0.0",
        "docker",
        "tqdm",
    ],
    entry_points={
        'console_scripts': [
            'swebcs=swe_sharp_bench.cli:cs_main',
        ],
    },
    python_requires=">=3.10",
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
    ],
)
