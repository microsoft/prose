#!/usr/bin/env python3
"""
Convert CSV dataset to JSON format required by SWE-bench evaluation harness.
This script adds missing fields that are required by the evaluation system.
"""

import csv
import json
import ast

def extract_pull_number(instance_id):
    """Extract pull number from instance_id (usually the last part after the last dash)"""
    parts = instance_id.split("-")
    return parts[-1] if parts else "0"

def fix_json_string_field(field_value):
    """Fix fields that contain JSON arrays as strings"""
    if isinstance(field_value, str):
        try:
            # Try to evaluate the string as a Python literal
            return ast.literal_eval(field_value)
        except (ValueError, SyntaxError):
            # If it fails, keep it as string
            return field_value
    return field_value

def convert_csv_to_json(csv_path, output_path):
    """Convert CSV to JSON format with all required fields"""
    data = []
    
    print(f"Reading CSV from: {csv_path}")
    
    with open(csv_path, 'r', encoding='utf-8') as csvfile:
        # Use a large field size limit for large patches
        csv.field_size_limit(10000000)
        reader = csv.DictReader(csvfile)
        
        for row in reader:
            instance = {}
            
            # Copy all existing fields
            for key, value in row.items():
                instance[key] = value
            
            # Add missing fields derived from existing data
            instance_id = instance.get('instance_id', '')
            
            # Extract pull_number from instance_id
            instance['pull_number'] = extract_pull_number(instance_id)
            
            # Fix JSON string fields (PASS_TO_PASS, FAIL_TO_PASS)
            instance['PASS_TO_PASS'] = fix_json_string_field(instance.get('PASS_TO_PASS', '[]'))
            instance['FAIL_TO_PASS'] = fix_json_string_field(instance.get('FAIL_TO_PASS', '[]'))
            
            # Add default version if missing
            if not instance.get('version'):
                instance['version'] = '0.1'
            
            # Convert empty strings to appropriate defaults
            for field in ['hints_text', 'problem_statement']:
                if not instance.get(field):
                    instance[field] = ''
            
            data.append(instance)
    
    print(f"Processed {len(data)} instances")
    
    # Write as JSON array
    print(f"Writing JSON to: {output_path}")
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    print(f"Successfully converted {len(data)} instances to JSON format")
    
    # Print sample for verification
    if data:
        print("\nSample instance fields:")
        sample = data[0]
        for key, value in sample.items():
            value_repr = repr(value)[:100] + "..." if len(repr(value)) > 100 else repr(value)
            print(f"  {key}: {value_repr}")

if __name__ == "__main__":
    csv_path = "../data/benchmark/swe-sharp-bench.csv"
    json_path = "data/tasks/swe-sharp-bench.json"
    
    # Create directory if it doesn't exist
    import os
    os.makedirs(os.path.dirname(json_path), exist_ok=True)
    
    convert_csv_to_json(csv_path, json_path)
