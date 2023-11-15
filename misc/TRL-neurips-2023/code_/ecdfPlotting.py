from typing import Options
import os
import matplotlib.pyplot as plt
import numpy as np


def compute_ecdf(data):
        """Compute ECDF for a one-dimensional array of measurements."""
        n = len(data)
        x = np.sort(data)
        y = np.arange(1, n+1) / n
        return x, y

def ecdf_scatter_plot(attributes, save_path,pivot_table,name_save, type:str= Options['dots',"step"], figsize:tuple=(30,20), k:int=1,temperature:float=0.1):
    """With table formats as columns and test cases as row. ecdf plot across table manipulation for a specific temperature and metric """
    # Define the number of subplots and their arrangement
    x_list = [[test, "", temperature ] for test in attributes["TestCase"] ]
    num_rows = len(x_list)
    num_cols = 6

    # Create a figure and a grid of subplots
    fig, axes = plt.subplots(num_rows, num_cols, figsize=figsize)
    
    # Loop through each subplot and plot the scatter plot
    for i in range(num_rows):
        for j in range(num_cols):
            
            TableFormat = attributes["tableFormat"][j]
            ax = axes[i][j]
            # Filter data for each subplot based on data_labels
            for manipulation in attributes["TableManipulation"]:
                x= x_list[i]
                x[1] = manipulation
                x = tuple(x)
                
                y = ( f'pass_{k}', TableFormat)
                scores = pivot_table.loc[x][y]
                scores_x, scores_y = compute_ecdf(scores)
                if type == "dots":
                    ax.plot(scores_x, scores_y, label=f'{manipulation}',marker = ".", linestyle = "none")
                else:
                    ax.step(scores_x, scores_y, label=f'{manipulation}')
           
            name = f'Test: {x[0]}, metric: pass@{k}\nTemperature: {x[2]}, Format: {y[1]}'
            
            ax.legend()
            ax.set_title(name)
            ax.set_xlabel('score (%)')
            ax.set_ylabel('ecdf')
            

    # Adjust layout to prevent overlapping labels
    plt.tight_layout()
    plt.savefig(os.path.join(save_path, name_save.replace(".csv", "_") +f"ecdf_{type}_plot.pdf"))
    # Show the plot
    plt.show()
    plt.clf()