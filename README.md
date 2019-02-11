# PROSE Website

This repository contains documents and templates used to build and deploy the PROSE website (https://microsoft.github.io/prose). The website is hosted using GitHub Pages. Therefore, the deployment process involves pushing to a branch called `gh-pages` on the repository associated with our public code samples.

## How to setup
Follows [the official Github Pages instructions](https://help.github.com/articles/setting-up-your-github-pages-site-locally-with-jekyll/).

1. Install Ruby: `cinst -y ruby`
2. Install Jekyl Bundler: `gem install jekyll bundler`
3. Reopen your shell to pick up the new PATH, and run:

    ``` bash
    git clone https://github.com/Microsoft/prose prose-site
    cd prose-site
    git checkout gh-pages
    bundle install  # install the github-pages version of Jekyll and its dependencies
    npm ci          # download the frontend dependencies and build the JS/CSS files
    ```

## How to run locally

1. Make your change (likely to a markdown file in `/documentation`).
2. If you change any TS/JS/CSS files, run `gulp` and commit the compiled files in `/static`; otherwise, ignore this step.
3. `bundle exec jekyll serve --baseurl=''` (overriding the `baseURL` like this required for local viewing)
4. Open and monitor <http://localhost:4000/> in your browser.

You can also edit directly on GitHub for small changes.

## How to write

You can use any standard [GitHub-flavored Markdown syntax](https://guides.github.com/features/mastering-markdown/) when you write your docs, just keep in mind:  
 - Every page needs a `title` in its YAML front matter.
 - Include `{% include toc.liquid.md %}` if you want a table of contents.
 - Prepend every link with `{{ site.baseurl }}`.
 - **DO NOT** use the tab character (`\t`). Its width is inconsistent across browsers, even with all proper CSS styles in places. Before committing, make sure to convert all tabs to the desired number of spaces.
 - Don't forget to specify programming languages in the code snippets. That includes `csharp`, `powershell`, `xml`, `bash`, `json`, `python`, and `r` (among all others).

        ``` csharp
        // Code here
        ```
 - To mark DSL grammar snippets, don't put any language code after the backticks. Instead, put `{: .language-dsl}` immediately after the snippet:

        ```
        @start string P := Id(x);
        ```
        {: .language-dsl}


### Navigation

To add your page to the navigation menu, edit [menu.liquid.html](/_includes/menu.liquid.html).
Follow the markup patterns already present in the file.
