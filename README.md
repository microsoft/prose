This repository contains documents and templates used to build and deploy the PROSE website (http://microsoft.github.io/prose). The website is hosted using GitHub pages. Therefore, the deployment process involves pushing to a branch called gh-pages on the repository associated with our public code samples.

Steps to run while locally iterating on a website change:

 * Make your change (likely to a markdown file in /content/documentation)
 * Build Shim.sln in Visual Studio (only required if you made any TypeScript changes, commit compile JS if you do)
 * `hugo server --watch -b "http://localhost:1313/"` (overriding the `baseURL` like this required for local viewing)
 * Open and monitor http://localhost:1313/ in your browser (will auto-refresh for most changes)

Steps to deploy this site manually:

 * Build Shim.sln in Visual Studio (only required if you made any TypeScript changes, commit compile JS if you do)
 * `hugo` (this populates /public with a deployable static site)
 * `cd public`
 * `git init .`
 * `git add .`
 * `git commit -m "manual deployment of generated site"`
 * `git remote add origin git@github.com:Microsoft/prose.git`
 * `git push --force origin master:gh-pages`
 * `cd ..`
 
A powershell script called Deploy-AzureGit.ps1 automates the `cd` and `git`-related commands above. Run it from this diretory. You are still responsible for making sure /public is in a deployable state (e.g., it was generated without overriding hugo's baseURL).