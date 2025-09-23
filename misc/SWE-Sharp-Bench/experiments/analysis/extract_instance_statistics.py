#!/usr/bin/env python3
"""
Script to extract comprehensive statistics from SWE-Sharp-Bench data including:
1. Instance-level statistics (problem statements, patches, tests)
2. Repository metrics (files count and lines of code)

This script processes SWE-Sharp-Bench CSV data and optionally extracts repository metrics
by cloning repositories to get accurate file counts and lines of code.
"""

import pandas as pd
import json
import re
import os
import shutil
import subprocess
import tempfile
import csv
import time
import logging
from collections import defaultdict
import ast
from pathlib import Path
from typing import Dict, Set, List, Tuple

# Set up logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('swe_bench_statistics_extraction.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

class RepositoryMetricsExtractor:
    """Class to handle repository cloning and metrics extraction"""
    
    def __init__(self):
        self.temp_dir = None
    
    def setup_temp_directory(self):
        """Create temporary directory for cloning repositories"""
        self.temp_dir = tempfile.mkdtemp(prefix='swe_repo_metrics_')
        logger.info(f"Using temporary directory: {self.temp_dir}")
    
    def cleanup_temp_directory(self):
        """Clean up temporary directory"""
        if self.temp_dir and os.path.exists(self.temp_dir):
            try:
                shutil.rmtree(self.temp_dir)
                logger.info(f"Cleaned up temporary directory: {self.temp_dir}")
            except Exception as e:
                logger.warning(f"Failed to clean up temporary directory: {e}")
    
    def clone_repository(self, repo_url: str, clone_dir: str) -> bool:
        """Clone a repository to the specified directory"""
        try:
            # Construct GitHub URL if not already a URL
            if not repo_url.startswith(('http://', 'https://')):
                repo_url = f"https://github.com/{repo_url}.git"
            
            logger.info(f"Cloning {repo_url}...")
            
            # Clone with shallow clone for faster operation
            result = subprocess.run([
                'git', 'clone', '--depth', '1', repo_url, clone_dir
            ], capture_output=True, text=True, timeout=300)  # 5 minute timeout
            
            if result.returncode == 0:
                logger.info(f"Successfully cloned {repo_url}")
                return True
            else:
                logger.error(f"Failed to clone {repo_url}: {result.stderr}")
                return False
                
        except subprocess.TimeoutExpired:
            logger.error(f"Timeout cloning {repo_url}")
            return False
        except Exception as e:
            logger.error(f"Error cloning {repo_url}: {e}")
            return False
    
    def count_files_and_loc(self, repo_dir: str) -> Tuple[int, int]:
        """Count C# files and lines of code in the repository"""
        try:
            extensions = ['.cs']  # Focus on C# files for SWE-Sharp-Bench
            
            total_files = 0
            total_loc = 0
            
            # Walk through all files in the repository
            for root, dirs, files in os.walk(repo_dir):
                # Skip common non-source directories
                dirs[:] = [d for d in dirs if not d.startswith('.') and 
                          d not in ['node_modules', 'target', 'build', 'bin', 'obj', '__pycache__', 
                                   'packages', '.nuget', '.vs']]
                
                for file in files:
                    file_path = os.path.join(root, file)
                    
                    # Check if file has relevant extension
                    if any(file.endswith(ext) for ext in extensions):
                        total_files += 1
                        
                        # Count lines of code
                        try:
                            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                                lines = f.readlines()
                                # Count non-empty lines (basic LoC counting)
                                non_empty_lines = sum(1 for line in lines if line.strip())
                                total_loc += non_empty_lines
                        except Exception as e:
                            logger.warning(f"Could not read file {file_path}: {e}")
                            continue
            
            logger.info(f"Found {total_files} C# files with {total_loc} lines of code")
            return total_files, total_loc
            
        except Exception as e:
            logger.error(f"Error counting files and LoC in {repo_dir}: {e}")
            return 0, 0
    
    def get_repository_metrics(self, repo_name: str) -> Tuple[int, int]:
        """Get repository metrics (files count and LoC)"""
        if not self.temp_dir:
            self.setup_temp_directory()
        
        # Create temporary directory for this repository
        repo_dir = os.path.join(self.temp_dir, repo_name.replace('/', '_'))
        
        try:
            # Clone the repository
            if not self.clone_repository(repo_name, repo_dir):
                return 0, 0
            
            # Count files and LoC
            files, loc = self.count_files_and_loc(repo_dir)
            return files, loc
            
        except Exception as e:
            logger.error(f"Error processing {repo_name}: {e}")
            return 0, 0
        finally:
            # Clean up cloned repository
            if os.path.exists(repo_dir):
                try:
                    shutil.rmtree(repo_dir)
                    logger.info(f"Cleaned up {repo_dir}")
                except Exception as e:
                    logger.warning(f"Failed to clean up {repo_dir}: {e}")

def safe_eval_list(list_str):
    """Safely evaluate string representation of list"""
    try:
        if pd.isna(list_str) or list_str == '':
            return []
        # Handle different string formats
        if isinstance(list_str, str):
            # Remove extra whitespace and handle various formats
            list_str = list_str.strip()
            if list_str.startswith('[') and list_str.endswith(']'):
                return ast.literal_eval(list_str)
            else:
                # Handle comma-separated values without brackets
                return [item.strip() for item in list_str.split(',') if item.strip()]
        return list_str if isinstance(list_str, list) else []
    except:
        return []

def count_words_and_tokens(text):
    """Count words and estimate tokens from text"""
    if pd.isna(text) or text == '':
        return 0, 0
    
    words = len(text.split())
    # Approximate tokens as words * 1.3 (common approximation for English text)
    tokens = int(words * 1.3)
    return words, tokens

def count_lines_in_patch(patch):
    """Count number of changed lines in a patch (additions + deletions)"""
    if pd.isna(patch) or patch == '':
        return 0
    
    lines = patch.split('\n')
    changed_lines = 0
    
    for line in lines:
        # Count lines that start with + or - (but not +++ or ---)
        if line.startswith('+') and not line.startswith('+++'):
            changed_lines += 1
        elif line.startswith('-') and not line.startswith('---'):
            changed_lines += 1
    
    return changed_lines

def count_hunks_in_patch(patch):
    """Count number of hunks in a patch (sections starting with @@)"""
    if pd.isna(patch) or patch == '':
        return 0
    return len(re.findall(r'@@.*?@@', patch))

def count_files_in_patch(patch):
    """Count number of files mentioned in a patch"""
    if pd.isna(patch) or patch == '':
        return 0
    # Look for file headers like "--- a/file" or "+++ b/file"
    file_patterns = re.findall(r'(?:---|\+\+\+)\s+[ab]/(.+)', patch)
    return len(set(file_patterns))

def process_swe_data(csv_file_path, extract_repo_metrics=False):
    """Process SWE-Sharp-Bench data and generate comprehensive statistics"""
    
    # Read the CSV file
    df = pd.read_csv(csv_file_path)
    
    # Initialize repository metrics extractor if needed
    repo_extractor = None
    if extract_repo_metrics:
        repo_extractor = RepositoryMetricsExtractor()
        logger.info("Repository metrics extraction enabled")
    
    # Initialize dictionaries for statistics
    repo_stats = defaultdict(lambda: {
        'instances': [],
        'word_counts': [],
        'token_counts': [],
        'gold_patch_lines': [],
        'test_patch_lines': [],
        'gold_patch_hunks': [],
        'test_patch_hunks': [],
        'gold_patch_files': [],
        'test_patch_files': [],
        'f2p_counts': [],
        'p2p_counts': []
    })
    
    instance_data = {}
    unique_repos = set()
    
    # First pass: collect all unique repositories
    for _, row in df.iterrows():
        unique_repos.add(row['repo'])
    
    logger.info(f"Found {len(unique_repos)} unique repositories")
    
    # Extract repository metrics if requested
    repo_metrics = {}
    if extract_repo_metrics and repo_extractor:
        logger.info("Extracting repository metrics...")
        total_repos = len(unique_repos)
        
        for i, repo in enumerate(unique_repos, 1):
            logger.info(f"Processing repository {i}/{total_repos}: {repo}")
            files, loc = repo_extractor.get_repository_metrics(repo)
            repo_metrics[repo] = {'files': files, 'loc': loc}
            
            # Small delay to be respectful to GitHub
            time.sleep(1)
        
        # Clean up temporary directory
        repo_extractor.cleanup_temp_directory()
    
    # Process each instance row
    for _, row in df.iterrows():
        instance_id = row['instance_id']
        repo = row['repo']
        
        # Count words and tokens in problem statement
        words, tokens = count_words_and_tokens(row['problem_statement'])
        
        # Count lines in patches
        gold_patch_lines = count_lines_in_patch(row['patch'])
        test_patch_lines = count_lines_in_patch(row['test_patch'])
        
        # Count hunks in patches
        gold_patch_hunks = count_hunks_in_patch(row['patch'])
        test_patch_hunks = count_hunks_in_patch(row['test_patch'])
        
        # Count files in patches
        gold_patch_files = count_files_in_patch(row['patch'])
        test_patch_files = count_files_in_patch(row['test_patch'])
        
        # Process FAIL_TO_PASS and PASS_TO_PASS lists
        f2p_list = safe_eval_list(row['FAIL_TO_PASS'])
        p2p_list = safe_eval_list(row['PASS_TO_PASS'])
        
        f2p_count = len(f2p_list)
        p2p_count = len(p2p_list)
        
        # Store individual instance data
        instance_data[instance_id] = {
            'repo': repo,
            'base_commit': row['base_commit'],
            'problem_statement_words': words,
            'problem_statement_tokens': tokens,
            'gold_patch_lines': gold_patch_lines,
            'test_patch_lines': test_patch_lines,
            'gold_patch_hunks': gold_patch_hunks,
            'test_patch_hunks': test_patch_hunks,
            'gold_patch_files': gold_patch_files,
            'test_patch_files': test_patch_files,
            'fail_to_pass_count': f2p_count,
            'pass_to_pass_count': p2p_count,
            'fail_to_pass_tests': f2p_list,
            'pass_to_pass_tests': p2p_list
        }
        
        # Accumulate repository statistics
        repo_stats[repo]['instances'].append(instance_id)
        repo_stats[repo]['word_counts'].append(words)
        repo_stats[repo]['token_counts'].append(tokens)
        repo_stats[repo]['gold_patch_lines'].append(gold_patch_lines)
        repo_stats[repo]['test_patch_lines'].append(test_patch_lines)
        repo_stats[repo]['gold_patch_hunks'].append(gold_patch_hunks)
        repo_stats[repo]['test_patch_hunks'].append(test_patch_hunks)
        repo_stats[repo]['gold_patch_files'].append(gold_patch_files)
        repo_stats[repo]['test_patch_files'].append(test_patch_files)
        repo_stats[repo]['f2p_counts'].append(f2p_count)
        repo_stats[repo]['p2p_counts'].append(p2p_count)
    
    # Calculate repository-level statistics
    summary_data = []
    
    for repo, stats in repo_stats.items():
        # Base row data with instance statistics
        row_data = {
            'Org/Repo': repo,
            'Repository': repo.split('/')[-1] if '/' in repo else repo,  # Extract repo name
            '#Count': len(stats['instances']),
            'Avg. #Tokens': round(sum(stats['token_counts']) / len(stats['token_counts']), 2) if stats['token_counts'] else 0,
            'Avg. #Lines': round(sum(stats['gold_patch_lines']) / len(stats['gold_patch_lines']), 2) if stats['gold_patch_lines'] else 0,
            'Avg. #Hunks': round(sum(stats['gold_patch_hunks']) / len(stats['gold_patch_hunks']), 2) if stats['gold_patch_hunks'] else 0,
            'Avg. #Files': round(sum(stats['gold_patch_files']) / len(stats['gold_patch_files']), 2) if stats['gold_patch_files'] else 0,
            'Test Patch Avg. #Lines': round(sum(stats['test_patch_lines']) / len(stats['test_patch_lines']), 2) if stats['test_patch_lines'] else 0,
            'Test Patch Avg. #Hunks': round(sum(stats['test_patch_hunks']) / len(stats['test_patch_hunks']), 2) if stats['test_patch_hunks'] else 0,
            'Test Patch Avg. #Files': round(sum(stats['test_patch_files']) / len(stats['test_patch_files']), 2) if stats['test_patch_files'] else 0,
            '#Avg. F2P': round(sum(stats['f2p_counts']) / len(stats['f2p_counts']), 2) if stats['f2p_counts'] else 0,
            '#Avg. P2P': round(sum(stats['p2p_counts']) / len(stats['p2p_counts']), 2) if stats['p2p_counts'] else 0
        }
        
        # Add repository metrics if available
        if repo in repo_metrics:
            metrics = repo_metrics[repo]
            row_data['#LoC'] = f"{metrics['loc'] / 1000:.1f}k" if metrics['loc'] >= 1000 else str(metrics['loc'])
            row_data['#Files'] = metrics['files']
        else:
            # Default values when repository metrics are not extracted
            row_data['#LoC'] = 'N/A'
            row_data['#Files'] = 'N/A'
        
        # Add unit test columns (matching the table structure in the image)
        row_data['Unit tests'] = 'N/A'  # This would need to be calculated from actual test analysis
        
        summary_data.append(row_data)
    
    # Create summary DataFrame and sort by repository name
    summary_df = pd.DataFrame(summary_data)
    summary_df = summary_df.sort_values('Org/Repo')
    
    return summary_df, instance_data, repo_metrics

def main():
    """Main function"""
    import argparse
    
    # Set up command line arguments
    parser = argparse.ArgumentParser(description='Extract comprehensive SWE-Sharp-Bench statistics')
    parser.add_argument('--csv-file', 
                       default='../../data/benchmark/swe-sharp-bench-msbench-file.csv',
                       help='Path to the SWE-Sharp-Bench CSV file')
    parser.add_argument('--extract-repo-metrics', 
                       action='store_true',
                       help='Extract repository metrics by cloning repositories (slower but more accurate)')
    parser.add_argument('--output-prefix',
                       default='swe_sharp_bench',
                       help='Prefix for output files')
    
    args = parser.parse_args()
    
    # Resolve file path
    csv_file_path = args.csv_file
    if not os.path.isabs(csv_file_path):
        # Make relative path relative to script location
        script_dir = os.path.dirname(os.path.abspath(__file__))
        csv_file_path = os.path.join(script_dir, csv_file_path)
    
    print("Processing SWE-Sharp-Bench data...")
    logger.info(f"Input file: {csv_file_path}")
    logger.info(f"Extract repository metrics: {args.extract_repo_metrics}")
    
    try:
        # Process the data
        summary_df, instance_data, repo_metrics = process_swe_data(
            csv_file_path, 
            extract_repo_metrics=args.extract_repo_metrics
        )
        
        # Save summary statistics to CSV (matching the table format from the image)
        output_csv = f'{args.output_prefix}_comprehensive_statistics.csv'
        
        # Reorder columns to match the table structure
        column_order = [
            'Org/Repo', 'Repository', '#Files', '#LoC', '#Count', 'Avg. #Tokens', 
            'Avg. #Lines', 'Avg. #Hunks', 'Avg. #Files',
            'Test Patch Avg. #Lines', 'Test Patch Avg. #Hunks', 'Test Patch Avg. #Files',
            '#Avg. F2P', '#Avg. P2P', 'Unit tests'
        ]
        
        # Reorder columns if they exist
        existing_columns = [col for col in column_order if col in summary_df.columns]
        summary_df = summary_df[existing_columns]
        
        summary_df.to_csv(output_csv, index=False)
        print(f"Comprehensive repository statistics saved to: {output_csv}")
        logger.info(f"Comprehensive repository statistics saved to: {output_csv}")
        
        # Save individual instance data to JSON
        output_json = f'{args.output_prefix}_instances.json'
        with open(output_json, 'w') as f:
            json.dump(instance_data, f, indent=2)
        print(f"Individual instance data saved to: {output_json}")
        logger.info(f"Individual instance data saved to: {output_json}")
        
        # Save repository metrics if extracted
        if args.extract_repo_metrics and repo_metrics:
            metrics_json = f'{args.output_prefix}_repository_metrics.json'
            with open(metrics_json, 'w') as f:
                json.dump(repo_metrics, f, indent=2)
            print(f"Repository metrics saved to: {metrics_json}")
            logger.info(f"Repository metrics saved to: {metrics_json}")
        
        # Display summary
        print(f"\nProcessed {len(instance_data)} instances across {len(summary_df)} repositories")
        logger.info(f"Processed {len(instance_data)} instances across {len(summary_df)} repositories")
        
        print("\nFirst few rows of comprehensive statistics:")
        print(summary_df.head().to_string(index=False))
        
        if args.extract_repo_metrics:
            successful_metrics = sum(1 for r in repo_metrics.values() if r['files'] > 0 or r['loc'] > 0)
            print(f"\nRepository metrics extraction:")
            print(f"Successfully processed: {successful_metrics}/{len(repo_metrics)} repositories")
            
            if successful_metrics > 0:
                total_files = sum(r['files'] for r in repo_metrics.values())
                total_loc = sum(r['loc'] for r in repo_metrics.values())
                print(f"Total C# files: {total_files:,}")
                print(f"Total lines of code: {total_loc:,}")
        
    except FileNotFoundError:
        print(f"Error: Could not find the CSV file '{csv_file_path}'")
        print("Please make sure the file path is correct and the file exists.")
        logger.error(f"CSV file not found: {csv_file_path}")
    except Exception as e:
        print(f"Error processing data: {str(e)}")
        logger.error(f"Error processing data: {str(e)}", exc_info=True)

if __name__ == "__main__":
    main()