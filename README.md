ScriptGenerator
===============

.NET Tool used to create OpenSTA script or JMeter script from Fiddler sessions.


Two tools are in this project:
1 - FiddlerSessionComparer
2 - GeneratorFramework

1 - FiddlerSessionComparer
---------------------------
FiddlerSessionComparer creates a 'Page' structure from two or more (not yet available) FiddlerSessions files. 
Each HTTP request (or Fiddler Session) will correspond to a 'Page', and one 'Page' will contain one HTTP request.
Each 'Page' knows his "parent" 'Page', and its "Followers" 'Pages', creating a three structure of Pages.

After comparing two HTTP requests from the two FiddlerSessions file, the comparer will detect differences in the parameters. 
Each difference will be a 'Parameter'. The value of the parameter is known in the response of the "parent" 'Page', and its going 
to be used in one or more Pages. 
So, each 'Page' has a list of Parameters to Extract, and a list of Parameters to Use. 
And each Parameter will know where and how to be extracted, and where (page and section: {body, header, url}) and how to be used (how to replace the currect value with the parameter))

Now some new features are supported:
- a parameter can be used in different contexts in different pages. For example: Parameter Key=name, Value=Simon, is used as parameter in an URL as CSV, and also inside a POST BODY. The comparer will reuse the parameter for both situations. That means, now differences in parameters in URLs are detected.
- Complex structures are now well managed. For example, a parameter in a body where its value is a JSON, that contains an XML somewhere inside. Now the comparar will extract the leads of the parameters structure and use that to compare. JSON and XML formats are supported.

Also a cup of test cases where added to the solutions. We will add more in the future.


 
2 - GeneratorFramework
---------------------------
Until now, it just generates to JMeter script. But it'll generate to OpenSTA too, after we merge this tool with other code we have in Abstracta.
Using comments to group HTTP Requests in the FiddlerSession file, it'll create "steps" in the script, that will contain the requests of the group. 
It'll also group the Redirects, and the "Secondary Requests" of each "Main Request". Secondary Requests are images, css, js, and all static content.
The generator also creates validations for the requests, and adds some logic to help "debuggin" the script.

When combining with the FiddlerSessionComparer (using two FiddlerSessions instead of just one), the Generator creates RegExp Extractors to extract 
values from one response and to use them in the next request.
It does all the magic! :)

It also has some bugs, and maybe it doesn't support all the cases in the world... but please send us ur cases, or just fork the project and add the code that you need.


