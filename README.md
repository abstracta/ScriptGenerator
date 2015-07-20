ScriptGenerator
===============

This project started with the aim of simplify the way we make scripts in JMeter. There are a lot of ways to create a JMeter script, and we find one that we think is awesome :). Also, we realized that we could extend this tool, not only for JMeter, and we are trying to include OpenSTA support.

Basically we have two main tools in this solution:
1 - FiddlerSessionComparer
2 - GeneratorFramework

1 - FiddlerSessionComparer
---------------------------
Fiddler Session Comparer (FSC) creates a tree view structure based on the "Fiddler sessions". As we know, when we request a page, the page invokes some resources (images, css, js) that it needs to show itself. So, we would say that the first HTTP request is the primary request and the other ones corresponding to additional resources like images, js and css are secondary requests. So, in this manner we would create a tree view for HTTP request.

FCS uses from two or more (nowadays, just two, we are working to add more) Fiddler Sessions files. After comparing two HTTP requests from Fiddler Sessions files, the comparer detect differences between requests. Each difference is a 'Parameter'. This parameters are reused along all the analysis.
 
2 - GeneratorFramework
---------------------------

So far it just generates JMeter scripts, but it will generate OpenSTA scripts too, after we merge this tool with another one we have in Abstracta. Using comments to group HTTP Requests in the Fiddler Session file, the tool creates "steps" in the script a group of requests. It'll also group the Redirects and the "Secondary Requests" of each "Main Request" (images, css, js, and all static content are considered Secondary Requests). The generator also creates validations for the requests, and adds some logic to help "debugging" the script.

When combining it with the FiddlerSessionComparer (using two FiddlerSessions instead of just one), the Generator creates RegExp Extractors to extract values from one response and to use them in the next request. It does all the magic! :)

It also has some bugs, and maybe it doesn't support all the cases in the world... but please send us your cases, or just fork the project and add the code that you need.


