ScgiSharp
==========

SCGI server written in C# with host for NancyFx

How to use
====
See examples in Sandbox folder
For libevent you need  Oars.dll.config with something like that
 
    <configuration>
        <dllmap dll="event_core"  target="/usr/lib/x86_64-linux-gnu/libevent_core-2.0.so.5.1.7" />
    </configuration>



Configuration with nginx
=======

    location /scgi/
    {
         scgi_pass 192.168.56.42:10081;
         include /etc/nginx/scgi_params;
         scgi_param      REQUEST_ROOT /scgi/;
    }


`REQUEST_ROOT` is prefix which will be removed from url Path.

