from typing import Options
import os
import matplotlib.pyplot as plt
import numpy as np
from collections import Counter



def scatter_plot(attributes, save_path,pivot_table,name_save):
    x_list = [(test, manipulation, temp ) for test in attributes["TestCase"] for manipulation in attributes["TableManipulation"] for temp in attributes["temperature"]]
    # Define the number of subplots and their arrangement

    num_rows = len(x_list)
    num_cols = 6

    # Create a figure and a grid of subplots
    fig, axes = plt.subplots(num_rows, num_cols, figsize=(50,500))


    # Loop through each subplot and plot the scatter plot
    for i in range(num_rows):
        for j in range(num_cols):
            
            TableFormat = attributes["tableFormat"][j]
            ax = axes[i][j]
            markerss= [".", "x", "p", "^","o","s"]
            # Filter data for each subplot based on data_labels
            for j, k in enumerate([1,3,5,10,15]):
                x= x_list[i]
                y = ( f'pass_{k}', TableFormat)
                scores = pivot_table.loc[x][y]
                ax.scatter(range(len(scores)), scores, label=f'pass@{k}', s=int(300/k), marker =markerss[j] )
            name = f'Test: {x[0]}, TableManipulation: {x[1]}\nTemperature: {x[2]}, Format: {y[1]}'
            ax.set_title(name)
            ax.set_xlabel('data-points')
            ax.set_ylabel('score(%)')
            
    ax.legend()
    # Adjust layout to prevent overlapping labels
    plt.tight_layout()
    plt.savefig(os.path.join(save_path, name_save.replace(".csv", "_") +"_scatter_plot.pdf"))
    # Show the plot
    plt.show()
    plt.clf()
    
    
def density_scatter_plot(attributes, save_path,pivot_table,name_save):
    x_list = [(test, manipulation, temp ) for test in attributes["TestCase"] for manipulation in attributes["TableManipulation"] for temp in attributes["temperature"]]
    # Define the number of subplots and their arrangement

    num_rows = len(x_list)
    num_cols = 6

    # Create a figure and a grid of subplots
    fig, axes = plt.subplots(num_rows, num_cols, figsize=(50,500))


    # Loop through each subplot and plot the scatter plot
    for i in range(num_rows):
        for j in range(num_cols):
            
            TableFormat = attributes["tableFormat"][j]
            ax = axes[i][j]
            markerss= [".", "x", "p", "^","o","s"]
            # Filter data for each subplot based on data_labels
            for j, k in enumerate([1,3,5,10,15]):
                x= x_list[i]
                y = ( f'pass_{k}', TableFormat)
                scores = pivot_table.loc[x][y]
                counter_scores = Counter(scores)
                scores_x= counter_scores.keys()
                scores_y= counter_scores.values()
                ax.scatter(scores_x, scores_y, label=f'pass@{k}', s=int(300/k), marker =markerss[j] )
            name = f'Test: {x[0]}, TableManipulation: {x[1]}\nTemperature: {x[2]}, Format: {y[1]}'
            ax.set_xlim([-1,101])
            ax.set_title(name)
            ax.set_xlabel('score (%)')
            ax.set_ylabel('frequency')
             
    ax.legend()
    # Adjust layout to prevent overlapping labels
    plt.tight_layout()
    plt.savefig(os.path.join(save_path, name_save.replace(".csv", "_") +"density_scatter_plot.pdf"))
    # Show the plot
    plt.show()
    plt.clf()
    