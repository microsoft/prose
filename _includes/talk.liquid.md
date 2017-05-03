{% capture slides %} <a href="{{ include.slides }}" aria-label="Slides" title="Slides"><i class="fa fa-file-powerpoint-o"></i></a> {% endcapture %}
{% capture video %} {% if include.video %}<a href="{{ include.video }}" aria-label="Video" title="Video"><i class="fa fa-youtube-play"></i></a>{% endif %} {% endcapture %}
<li>
<div class="h3 mb-1 mt-4 subtitle" markdown="1">
{{ include.title }}{% if include.venue %}&ensp; <span class="h6">{{ include.venue }}</span>{% endif %} &nbsp; {{ slides }} &nbsp; {{ video }}
</div>
<div class="mt-1" markdown="1">
*{{ include.authors }}*
</div>
</li>
