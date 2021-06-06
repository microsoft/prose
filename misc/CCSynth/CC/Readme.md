# Set up a virtual environment and ipython kernel

Before starting the steps, change your current directory to `CC`. Then execute the following commands to set up a
virtual environment and ipython kernel.

```
python -m venv venv
source ./venv/Scripts/activate
pip install ipykernel
python -m ipykernel install --name=venv
```

# Install dependencies
```
pip install matplotlib
pip install scikit-learn
pip install jupyterlab
pip install runipy
pip install pdfkit
pip install -e DataInsights
```

# Download datasets
Download the datasets as zip file from [here](https://drive.google.com/drive/folders/1gBZuhV42VHhrwsiKpEA4dLYVdiiXbUGp?usp=sharing). We will assume that you have downloaded the datasets under the directory `CC/Datasets`.

# Extract datasets
```
mkdir Datasets/uncompressed
unzip -o Datasets/2008_14col -d Datasets/uncompressed/
unzip -o Datasets/EVL -d Datasets/uncompressed/
unzip -o Datasets/har -d Datasets/uncompressed/
```

This should set up everything you need to execute the ipython 
notebooks, which will reproduce the experimental results 
reported in the paper. Use the command `jupyter lab` to get 
started and view/execute the notebooks. Select the kernel 
`venv` from the notebook, which is the virtual environment you 
just set up.
