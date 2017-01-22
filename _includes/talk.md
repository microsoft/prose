{% capture slides %} <a href="{{ include.slides }}" aria-label="Slides" alt="Slides" title="Slides"><i class="fa fa-file-powerpoint-o"></i></a> {% endcapture %}
{% capture video %} {% if include.video %}<a href="{{ include.video }}" aria-label="Video" alt="Video" title="Video"><i class="fa fa-youtube-play"></i></a>{% endif %} {% endcapture %}
### **{{ include.title }}**{%if include.venue%}&ensp; <span class="h5">{{ include.venue }}</span>{%endif%} &nbsp; {{ slides }} &nbsp; {{ video }}
{: .mb-1 .subtitle}
*{{ include.authors }}*
