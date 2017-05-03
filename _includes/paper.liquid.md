{% capture pdf %} <a href="{{ include.pdf }}" aria-label="PDF" title="PDF"><i class="fa fa-file-pdf-o"></i></a> {% endcapture %}
{% capture slides %} {% if include.slides %}<a href="{{ include.slides }}" aria-label="Slides" title="Slides"><i class="fa fa-file-powerpoint-o"></i></a>{% endif %} {% endcapture %}
<li>
<div class="h3 mb-1 mt-4 subtitle" markdown="1">
{{ include.title }} &ensp; <span class="h6">{{ include.venue }}</span> &nbsp; {{ pdf }} &nbsp; {{ slides }}
</div>
<div class="mt-1" markdown="1">
*{{ include.authors }}*
</div>
</li>
