import os
import json
import jsonlines
import subprocess
from cli import main
from scrape_instances import read_json_file

def convert_jsonl_to_json(folder_path, json_files_folder_path):
    os.makedirs(json_files_folder_path, exist_ok=True)
    jsonl_files = [f for f in os.listdir(folder_path) if f.endswith(".jsonl")]
    
    for file in jsonl_files:
        jsonl_path = os.path.join(folder_path, file)
        json_filename = file.replace(".jsonl", ".json")
        json_path = os.path.join(json_files_folder_path, json_filename)
        
        data = []
        with jsonlines.open(jsonl_path) as reader:
            for obj in reader:
                obj["dotnet"] = True
                obj["version"] = "0.1"
                data.append(obj)
        
        with open(json_path, "w") as f:
            json.dump(data, f, indent=2)
        
        print(f"Converted {file} to {json_filename} (saved in {json_files_folder_path})")

def remove_dangling_images():
    try:
        # Get IDs of all dangling images
        result = subprocess.run(
            ['docker', 'images', '-f', 'dangling=true', '-q'], 
            capture_output=True, 
            text=True, 
            check=True
        )
        
        image_ids = result.stdout.strip().split('\n')
        
        if image_ids and image_ids[0]:
            print(f"Found {len(image_ids)} dangling images. Removing...")
            
            remove_result = subprocess.run(
                ['docker', 'rmi'] + image_ids,
                capture_output=True,
                text=True
            )
            
            print("Removal output:")
            print(remove_result.stdout)
            
            if remove_result.stderr:
                print("Errors:")
                print(remove_result.stderr)
                
            return True
        else:
            print("No dangling images found.")
            return False
            
    except subprocess.CalledProcessError as e:
        print(f"Error executing Docker command: {e}")
        print(f"Error output: {e.stderr}")
        return False

def process_json_files(folder_path):
    for file in os.listdir(folder_path):
        if file.endswith(".json"):
            file_path = os.path.join(folder_path, file)
            try:
                process_file(file_path)
                
            except Exception as e:
                print(f"Error processing file {file_path}: {e}")
                print(f"Skipping file {file_path} due to error.")
                raise e

def process_file(file_path):
    data = read_json_file(file_path)
    if not data:
        return 
    
    print(f"Processing file: {file_path}")
    all_instance_ids = []
    repo = None
    for record in data:
        instance_id = record.get("instance_id")
        repo = record.get("repo")
        all_instance_ids.append(instance_id)
    
    batch_size = 4
    already_resolved_instances_file = "resolved_instances.json"
    
    already_resolved_instances = read_json_file(already_resolved_instances_file) or []
    
    for i in range(0, len(all_instance_ids), batch_size):
        batch_instance_ids = all_instance_ids[i:i+batch_size]
        print(f"Processing batch {i//batch_size + 1}/{(len(all_instance_ids) + batch_size - 1)//batch_size} with {len(batch_instance_ids)} instances")
        
        repo_temp = repo.replace("/", "_")
        predictions_path = "gold"
        run_id = f"validate-gold-{repo_temp}-batch-{i//batch_size + 1}"
        dataset_name = file_path 
        force_rebuild = True
        namespace = ""
        cache_level = "env"
        clean = False
        open_file_limit = 4096
        timeout = 1_800
        rewrite_reports = False
        modal = False 
        split = "test"
        max_workers = 4
        
        report_file = f"{predictions_path}.{run_id}.json"
        print(f"Report file: {report_file}")
        if not os.path.exists(report_file):
            report_dir = os.path.dirname(report_file)
            if report_dir:
                os.makedirs(report_dir, exist_ok=True)
                
            with open(report_file, 'w') as f:
                json.dump({}, f)
        
        run_report = main(
            dataset_name=dataset_name,
            split=split,
            instance_ids=batch_instance_ids,
            predictions_path=predictions_path,
            run_id=run_id,
            force_rebuild=force_rebuild,
            cache_level=cache_level,
            clean=clean,
            open_file_limit=open_file_limit,
            timeout=timeout,
            rewrite_reports=rewrite_reports,
            namespace=namespace,
            max_workers=max_workers,
            modal=modal,
        )
        
        report = read_json_file(run_report)
        
        if report and "resolved_ids" in report:
            for resolved_id in report["resolved_ids"]:
                print(f"Resolved ID: {resolved_id}")
                if resolved_id not in already_resolved_instances:
                    already_resolved_instances.append(resolved_id)
        
            print(f"Total resolved instances: {len(already_resolved_instances)}")
            
            # Save after each batch
            with open(already_resolved_instances_file, "w") as f:
                json.dump(already_resolved_instances, f, indent=2)
        
        print("Cleaning up docker images after batch...")
        remove_dangling_images()

if __name__ == "__main__":
    folder_path = "data/tasks/"
    json_files_folder_path = "data/tasks/json_files/"  
    
    convert_jsonl_to_json(folder_path, json_files_folder_path)
    
    process_json_files(json_files_folder_path)

