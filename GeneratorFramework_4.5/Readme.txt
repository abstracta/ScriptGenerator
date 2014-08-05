GeneratorFramework is a tool to generate performance scripts to different tools using HTTP traffic recorded by fiddler.

It runs in .NET 3.5 and .NET 4.5. 3.5 is needed for the users that runs the app in Windows XP.

Future work: 
----------------------------------------------------------------------------
 
- Write OSTA generator 

- Automatic validations when JSON is Default Validations, improve this

- Fix bugs reported by mail

----------------------------------------------------------------------------




Version 2014.07.21
- Fix some issues

----------------------------------------------------------------------------

Version 2014.06.27 (not yet finished)
- Fix some issues
- Delete Common.dll dependency from FiddlerSessionComparer and from GeneratorFramework

----------------------------------------------------------------------------

Version 2014.06.26 (not yet finished)

- Allow using two fiddler sessions to compare and parametrize
* Replace in body of POST done
- Search the response of the referer request, to extract the URL_parameters that will be used in a future request
* E.G. When it's an IFrame

----------------------------------------------------------------------------

Version 2014.06.25 (not yet finished)

- Allow using two fiddler sessions to compare and parametrize
* Refactor FiddlerSessionComparer classes
* Replace in body of POST done

----------------------------------------------------------------------------

Version 2014.06.25 (not yet finished)

- Allow using two fiddler sessions to compare and parametrize
* Refactor FiddlerSessionComparer classes
* Upload with several bugs

----------------------------------------------------------------------------

Version 2014.06.24 (not yet finished)

- Allow using two fiddler sessions to compare and parametrize
* TestGenerator JMeter test plan includes parameters to extract

----------------------------------------------------------------------------

Version 2014.06.23 (not yet finished)

- Allow using two fiddler sessions to compare and parametrize
* TestGenerator working

----------------------------------------------------------------------------

Version 2014.06.20 (first version - not yet finished)

- Allow using two fiddler sessions to compare and parametrize: 
* compare them to find what parameters must be parametrized.
* change values in requests using those parameters
* add some kind of extractor to get the values for those parameters

- Search the response of the referer request, to extract the URL_parameters that will be used in a future request
* E.G. When it's an IFrame

- Add .NET 3.5 project versions
* Add .NET 3.5 assemblies 

----------------------------------------------------------------------------

Version 2014.06.17

- Add inteligent automatic validations -> { when HTML -> page title ; when JSON -> default validation }
- JMETER: Add logging of HTTP response when fail
* http://stackoverflow.com/questions/1515689/jmeter-how-to-log-the-full-request-for-a-failed-response

----------------------------------------------------------------------------

Version 2014.06.12

- When there is a redirect by javascript, get the parameters of the URL using regular expression, and use them in the next request 
* Fix, the Param wasn't replaced in the URL
- Add "if (${debug} == 0) => thinktimes" instead of just thinktimes
- Review reg expression, instead of "+", use "*" to allow empty values

----------------------------------------------------------------------------

Version 2014.05.29
- HTTP sample name without parameters
* 'GET http://localhost/pepito.html?param1=valor1&param2=valor2' -> 'GET http://localhost/pepito.html'
- Add parameter and "if (${debug} == 0) => secondary requests"
- When there is a redirect by javascript, get the parameters of the URL using regular expression, and use them in the next request 

----------------------------------------------------------------------------

 Version 2014.05.18
- Add a Response Assertion to the secondary requests (skip HTTP response code validation)

----------------------------------------------------------------------------

 Version 2014.05.11
- Fix serverName:port issue
- Add automatic validation for the Genexus redirects

----------------------------------------------------------------------------

 Version 2014.05.07
- Fix bug related to variableNames (iterations, threads, rumpUp)
- Fix bug related to get 'server' and 'path' from URL
- Fix index of Path issues

----------------------------------------------------------------------------

 Version 2014.05.06
- Add HTTPS support to JMeter Generator
- Filter "Tunnel to" lines of ZAS 
* Filter all non GET or POST http methods
- Support empty WebApp
- Add testCase: duckduckgo\

----------------------------------------------------------------------------

 Version 2014.04.24
- Add ASESP testCase
- Add support to creation of a step with only "secondary" requests

----------------------------------------------------------------------------

 Version 2014.04.22
- Add PedidosYa testCase
- Add AutoGeneration of GxTest XML file
* Now it can generate just from SAZ file (grouping by comments)
- Add support to request out of $SERVER\$WEBAPP URL scope

----------------------------------------------------------------------------

 Version 2014.04.01
- JMeter generator first version working
* Refactor validations

----------------------------------------------------------------------------

 Version 2014.03.14
- AbstractGenerator 
- First version of JMeter generator

----------------------------------------------------------------------------

