# PROSE Website

This repository contains documents and templates used to build and deploy the PROSE website (https://microsoft.github.io/prose). The website is hosted using GitHub pages. Therefore, the deployment process involves pushing to a branch called gh-pages on the repository associated with our public code samples.

One-time setup to install Jekyll (based on [these instructions](https://ntotten.com/2012/03/02/github-pages-with-jekyll-local-development-on-windows/)):
  * From [RubyInstaller](http://rubyinstaller.org/downloads/) get the latest x64 version of Ruby and the `mingw64-64` version of the Development Kit
  * Install the development kit by running `ruby dk.rb init` followed by `ruby dk.rb install` in the directory you extracted it to.
  * DO **NOT** run `gem install jekyll`
  * Instead run `gem install github-pages` to install the github-pages version of Jekyll and its dependencies

Steps to run while locally iterating on a website change:

 * Make your change (likely to a markdown file in `/documentation`)
 * Build `Shim.sln` in Visual Studio (only required if you made any TypeScript changes, commit compiled JS if you do)
 * `jekyll serve --baseurl ''` (overriding the `baseURL` like this required for local viewing)
 * Open and monitor http://localhost:4000/ in your browser (will auto-refresh for most changes)

Since the actual Jekyll run is done by GitHub pages, you can edit directly on GitHub for small changes.
