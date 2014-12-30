JSONLight
=========

JSON Lightweight Parser for C#.  

* How to  
  `JSONLight json = new JSONLight(jsontext);`  
  `Dictionary<string, object> dict = json.Parse();`  
  
  `string jsonSerialize = json.Serialize();`  
  
* Return Value  
  Top Level DictionaryKey is Empty.  
   `Dictionary< "", Dictionary<string,object> >`  
  
  Secound Level Dictionary is Key and Value of JsonObject.


