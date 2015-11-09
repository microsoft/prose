/** Generated TOC
    Stuart Langridge, July 2007
    
    ### More detailed documentation on usage can be found here:
    ### http://wiki.df.dreamhosters.com/wiki/Generated_toc
    
    Generate a table of contents, based on headings in the page.
    
    To place the TOC on the page, add
    
    <div id="generated-toc"></div>
    
    to the page where you want the TOC to appear. If this element
    is not present, the TOC will not appear.
    
    The TOC defaults to displaying all headings that are contained within
    the same element as it itself is contained in (or all headings on the
    page if you did not provide a generated-toc container). 
    
    To override this,
    provide a "highest heading" value by adding class="generate_from_h3"
    (or h2, h4, etc) to the container. This will include headings h3 through
    h6. (If unspecified, this will display all headings, as if 
    class="generate_from_h1" was specified.)
    
    To limit from the other site, add a 'generate_to' class, such as 
    "generate_to_h4".
    
    Note that the 'order' of headings goes from lower numbers to higher, so
    that you go "from" h1 "to" h6. So for example, 
    to limit headings to between h2 and h4, you can specify
    class="generate_from_h2 generate_to_h4"
    
    If you specify a generate_to that is below generate_from, it will be set 
    to equal generate_from.
    
    The TOC defaults to operating only on headings contained within the same
    element as it itself, i.e., in a page like this:
    
    <div>
      <div>
        <div id="generated-toc"></div>
        <h1>foo</h1>
        <h2>bar</h2>
      </div>
      <h1>quux</h1>
    </div>
    
    The "quux" heading will not appear in the TOC. To override this,
    add class="generate_for_page" to the container, which will process
    all headings on the page wheresoever they may be.
    
    ##################
    ## Modified by Daniel Folkinshteyn <nanotube@users.sourceforge.net>
    ## August 26, 2008
    ##################
    ## 
    ## List of changes:
    ## * Added list type specification for the TOC (configurable)
    ## * Add "back to top" links after each heading that is included in the TOC. (configurable)
    ## * Made default to show the toc rather than hide it, if there's no cookie.
    ## * Cosmetic improvements to skip and show/hide links
    ## * Add some more documentation and examples (see immediately below).
    ## * Add generate_to config
    
    Some further documentation:
    
    ========
    To exclude a heading from the TOC, add a "no-TOC" class to it. For example:
    <h2 class="no-TOC">This will not show up in the TOC</h2>
    A good place to use this is in specifying a title for your table of contents. For example, you might like the following:
    <div id="generated-toc">
        <h2 class="no-TOC">Table of Contents</h2>
    </div>
    
    ========
    To set the TOC list type (ordered or unordered list), give the div a class of "list_type_ul" or "list_type_ol". For example:
    <div id="generated-toc" class="list_type_ul"></div>
    Default is ordered list ('ol'). 
    
    ========
    To enable or disable "back to top" links in the body, give the div a class of "back_to_top_on" or "back_to_top_off". For example:
    <div id="generated-toc" class="back_to_top_off"></div>
    The default is "on" - back to top links will be generated after each heading included in the TOC.
    
    ========
    To specify multiple classes for the div, separate them with spaces. For example:
    <div id="generated-toc" class="generate_from_h3 generate_for_page list_type_ul"></div>
    
    ========
    For conflicting class specifications, the last specified class takes precedence. For example, for the following specification:
    <div id="generated-toc" class="generate_from_h3 generate_from_h2 list_type_ul list_type_ol"></div>
    The resulting TOC will be generated from h2, and use ordered list type ('ol'). 
    
*/

generated_toc = {
  generate: function() {
    // Identify our TOC element, and what it applies to
    generate_from = '0';
    generate_to = '6';
    generate_for = 'unset';
    list_type = 'ol'; // this is the default TOC list type
    back_to_top = 'on'; // this is the default setting for back to top links.
    tocparent = document.getElementById('toc'); // changed from generated-toc
    if (tocparent) {
      // there is a div class="generated-toc" in the document
      // dictating where the TOC should appear
      classes = tocparent.className.split(/\s+/);
      for (var i=0; i<classes.length; i++) {
        // if it specifies which heading level to generate from,
        // or what level to generate for, save these specifications
        if (classes[i].match(/^generate_from_h[1-6]$/)) {
          generate_from = classes[i].substr(classes[i].length-1,1);
        } else if (classes[i].match(/^generate_for_[a-z]+$/)) {
          generate_for = classes[i].match(/^generate_for_([a-z]+)$/)[1];
        } else if (classes[i].match(/^list_type_[a-z]+$/)) {
          list_type = classes[i].match(/^list_type_([a-z]+)$/)[1];
        } else if (classes[i].match(/^back_to_top_[a-z]+$/)) {
          back_to_top = classes[i].match(/^back_to_top_([a-z]+)$/)[1];
        } else if (classes[i].match(/^generate_to_h[1-6]$/)) {
          generate_to = classes[i].substr(classes[i].length-1,1);
        }
      }
    } else {
      // They didn't specify a TOC element; exit
      return;
    }
    
    // doesn't make sense to have generate_to less than generate_from
    // so just quietly set them to equal
    if (generate_to < generate_from) {
        generate_to = generate_from
    }
    
    // set top_node to be the element in the document under which
    // we'll be analysing headings
    if (generate_for == 'page') {
      top_node = document.getElementsByTagName('body');
    } else {
      // i.e., explicitly set to "parent", left blank (so "unset"),
      // or some invalid value
      top_node = tocparent.parentNode;
    }
    
    // If there isn't a specified header level to generate from, work
    // out what the first header level inside top_node is
    // and make that the specified header level
    if (generate_from == 0) {
      first_header_found = generated_toc.findFirstHeader(top_node);
      if (!first_header_found) {
        // there were no headers at all inside top_node!
        return;
      } else {
        generate_from = first_header_found.toLowerCase().substr(1);
      }
    }
        
    // add all levels of heading we're paying attention to to the
    // headings_to_treat dictionary, ready to be filled in later
    headings_to_treat = {}
    headings_to_treat["h" + generate_to] = ''
    for (var i=parseInt(generate_to) - 1; i>= parseInt(generate_from); i--) {
      headings_to_treat["h" + i] = '';
    }
    
    // get headings. We can't say 
    // getElementsByTagName("h1" or "h2" or "h3"), etc, so get all
    // elements and filter them ourselves
    // need to use .all here because IE doesn't support gEBTN('*')
    nodes = top_node.all ? top_node.all : top_node.getElementsByTagName('*');
    
    // put all the headings we care about in headings
    headings = [];
    for (var i=0; i<nodes.length;i++) {
      if (nodes[i].nodeName.toLowerCase() in headings_to_treat) {
        // if heading has class no-TOC, skip it
        if ((' ' + nodes[i].className + ' ').indexOf('no-TOC') != -1) {
          continue;
        }
        headings.push(nodes[i]);
      }
    }
    
    // make the basic elements of the TOC itself, ready to fill into
    
    cur_head_lvl = "h" + generate_from;
    cur_list_el = document.createElement(list_type);
    cur_list_el.style.display = "block";
    tocparent.appendChild(cur_list_el);
    
    // now walk through our saved heading nodes
    for (var i=0; i<headings.length; i++) {
      this_head_el = headings[i];
      this_head_lvl = headings[i].nodeName.toLowerCase();
      if (!this_head_el.id) {
        // if heading doesn't have an ID, give it one
        this_head_el.id = 'heading_toc_j_' + i;
        this_head_el.setAttribute('tabindex','-1');
      }
      
      while(this_head_lvl > cur_head_lvl) {
        // this heading is at a lower level than the last one;
        // create additional nested lists to put it at the right level

        // get the *last* LI in the current list, and add our new UL to it
        var last_listitem_el = null;
        for (var j=0; j<cur_list_el.childNodes.length; j++) {
          if (cur_list_el.childNodes[j].nodeName.toLowerCase() == 'li') {
            last_listitem_el = cur_list_el.childNodes[j];
          }
        }
        if (!last_listitem_el) {
          // there aren't any LIs, so create a new one to add the UL to
          last_listitem_el = document.createElement('li');
        }
        new_list_el = document.createElement(list_type);
        last_listitem_el.appendChild(new_list_el);
        cur_list_el.appendChild(last_listitem_el);
        cur_list_el = new_list_el;
        cur_head_lvl = 'h' + (parseInt(cur_head_lvl.substr(1,1)) + 1);
      }
      
      while (this_head_lvl < cur_head_lvl) {
        // this heading is at a higher level than the last one;
        // go back up the TOC to put it at the right level
        cur_list_el = cur_list_el.parentNode.parentNode;
        cur_head_lvl = 'h' + (parseInt(cur_head_lvl.substr(1,1)) - 1);
      }
      
      // create a link to this heading, and add it to the TOC
      li = document.createElement('li');
      a = document.createElement('a');
      a.href = '#' + this_head_el.id;
      for (var j=0; j<this_head_el.childNodes.length; j++) {
        a.appendChild(this_head_el.childNodes[j].cloneNode(true));
      }
      li.appendChild(a);
      cur_list_el.appendChild(li);
      
      // create a "back to top" link
      if (back_to_top == 'on'){
        newdiv = document.createElement("div");
        newdiv.innerHTML = "<a href='#beforetoc'>[back to top]</a>";
        this_head_el.parentNode.insertBefore(newdiv, this_head_el.nextSibling);
      }
    }
    
    // add an aftertoc paragraph as destination for the skip-toc link
    p = document.createElement('p');
    p.id = 'aftertoc';
    tocparent.appendChild(p);
    
    // add a beforetoc paragraph as destination for the back-to-top link
    if (back_to_top == 'on'){
      p = document.createElement('p');
      p.id = 'beforetoc';
      tocparent.parentNode.insertBefore(p, tocparent)
    }
    
    // go through the TOC and find all LIs that are "empty", i.e., contain
    // only ULs and no links, and give them class="missing"
    var alllis = tocparent.getElementsByTagName("li");
    for (var i=0; i<alllis.length; i++) {
      var foundlink = false;
      for (var j=0; j<alllis[i].childNodes.length; j++) {
        if (alllis[i].childNodes[j].nodeName.toLowerCase() == 'a') {
          foundlink = true;
        }
      }
      if (!foundlink) {
        alllis[i].className = "missing";
      } else {
        alllis[i].className = "notmissing";
      }
    }
    
  },
  
  innerText: function(el) {
    return (typeof(el.innerText) != 'undefined') ? el.innerText :
          (typeof(el.textContent) != 'undefined') ? el.textContent :
          el.innerHTML.replace(/<[^>]+>/g, '');
  },
  
  findFirstHeader: function(node) {
    // a recursive function which returns the first header it finds inside
    // node, or null if there are no functions inside node.
    var nn = node.nodeName.toLowerCase();
    if (nn.match(/^h[1-6]$/)) {
      // this node is itself a header; return our name
      return nn;
    } else {
      for (var i=0; i<node.childNodes.length; i++) {
        var subvalue = generated_toc.findFirstHeader(node.childNodes[i]);
        // if one of the subnodes finds a header, abort the loop and return it
        if (subvalue) return subvalue;
      }
      // no headers in this node at all
      return null;
    }
  },

  init: function() {
    // quit if this function has already been called
    if (arguments.callee.done) return;

    // flag this function so we don't do the same thing twice
    arguments.callee.done = true;

    generated_toc.generate();
  }
};

(function(i) {var u =navigator.userAgent;var e=/*@cc_on!@*/false; var st =
setTimeout;if(/webkit/i.test(u)){st(function(){var dr=document.readyState;
if(dr=="loaded"||dr=="complete"){i()}else{st(arguments.callee,10);}},10);}
else if((/mozilla/i.test(u)&&!/(compati)/.test(u)) || (/opera/i.test(u))){
document.addEventListener("DOMContentLoaded",i,false); } else if(e){     (
function(){var t=document.createElement('doc:rdy');try{t.doScroll('left');
i();t=null;}catch(e){st(arguments.callee,0);}})();}else{window.onload=i;}})(generated_toc.init);


