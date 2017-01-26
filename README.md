# PROSE Website

This repository contains documents and templates used to build and deploy the PROSE website (https://microsoft.github.io/prose). The website is hosted using GitHub Pages. Therefore, the deployment process involves pushing to a branch called `gh-pages` on the repository associated with our public code samples.

One-time setup to install Jekyll:

  * From [RubyInstaller](http://rubyinstaller.org/downloads/) get the latest x64 version of Ruby and the `mingw64-64` version of the Development Kit
  * Install the development kit by running `ruby dk.rb init` followed by `ruby dk.rb install` in the directory you extracted it to.
  * Run `bundle install` to install the github-pages version of Jekyll and its dependencies
  * Run `npm install` to install the frontend dependencies and build the JS/CSS files.

Steps to run while locally iterating on a website change:

 * Make your change (likely to a markdown file in `/documentation`)
 * Run `gulp` or `npm run build` if you change any TS/JS/CSS files. Commit the compiled files in `/static` if you do.
 * `bundle exec jekyll serve --baseurl=''` (overriding the `baseURL` like this required for local viewing)
 * Open and monitor http://localhost:4000/ in your browser

Since the actual Jekyll run is done by GitHub pages, you can edit directly on GitHub for small changes.
