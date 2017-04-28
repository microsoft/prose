{% capture pdf %} <a href="{{ include.pdf }}" aria-label="PDF" title="PDF"><i class="fa fa-file-pdf-o"></i></a> {% endcapture %}
{% capture slides %} {% if include.slides %}<a href="{{ include.slides }}" aria-label="Slides" title="Slides"><i class="fa fa-file-powerpoint-o"></i></a>{% endif %} {% endcapture %}
### **{{ include.title }}** &ensp; <span class="h5">{{ include.venue }}</span> &nbsp; {{ pdf }} &nbsp; {{ slides }}
{: .mb-1 .mt-4 .subtitle}
*{{ include.authors }}*
