using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JSONLight
{
    class JSONLight
    {
        private int _current = 0;
        private string _source = string.Empty;
        Dictionary<string, object> _jsonDict = new Dictionary<string,object>();
        private int _currentIndent = 0;

        delegate bool IsTerminate(char c);

        enum JSON_SPECIAL_CODE
        {
            LEFT_BRACE = '{',
            RIGHT_BRACE = '}',
            DOUBLE_QUOTE = '"',
            COMMA = ',',
            ARRAY_START = '[',
            ARRAY_END = ']',
            SEPARATER = ':',
            SPACE = ' ',
            CR = '\r',
            LF = '\n'
        }

        static string INDENT = "    ";

        enum PARSE_MODE
        {
            KEY_VALIDATE_MODE,
            VALUE_VALIDATE_MODE
        }

        static public class JSON_VARIABLE
        {
            public static readonly string TRUEFLG = "true";
            public static readonly string FALSEFLG = "false";
            public static readonly string NULL = "null";
        }

        public JSONLight(string json)
        {
            _source = json;
            int start = _source.IndexOf(Convert.ToChar(JSON_SPECIAL_CODE.LEFT_BRACE.GetHashCode()));
            if (start == -1){
                _current = json.Length;
            }
            else{
                _current = start;
            }
        }

        public Dictionary<string, object> Parse(){
            _jsonDict = ParseObject();
            return _jsonDict;
        }

        private Dictionary<string, object> ParseObject(){

            Dictionary<string, object> dict = new Dictionary<string, object>();
            string key = string.Empty;
            PARSE_MODE mode = PARSE_MODE.KEY_VALIDATE_MODE;

            IsTerminate terminateString = IsStringTerminate;
            IsTerminate terminateVariable = IsVariableTerminate;

            while (_current < _source.Length)
            {
                char c = ReadCharacter();

                if(c == JSON_SPECIAL_CODE.DOUBLE_QUOTE.GetHashCode()){
                    if (mode == PARSE_MODE.KEY_VALIDATE_MODE)
                    {
                        key = ParseValiable(terminateString);
                        Skip(Convert.ToChar(JSON_SPECIAL_CODE.SEPARATER));
                        mode = PARSE_MODE.VALUE_VALIDATE_MODE;
                    }else{
                        dict.Add(key, ParseValiable(terminateString));
                        mode = PARSE_MODE.KEY_VALIDATE_MODE;
                    }

                }else if (c == JSON_SPECIAL_CODE.LEFT_BRACE.GetHashCode()){                    
                    dict.Add(key, ParseObject());
                    mode = PARSE_MODE.KEY_VALIDATE_MODE;
                
                }else if (c == JSON_SPECIAL_CODE.ARRAY_START.GetHashCode()){
                    dict.Add(key, ParseArray());
                    mode = PARSE_MODE.KEY_VALIDATE_MODE;

                }else if (c == JSON_SPECIAL_CODE.RIGHT_BRACE.GetHashCode()){
                    break;

                }else if(c == JSON_SPECIAL_CODE.SPACE.GetHashCode()){

                }else{
                    if (mode == PARSE_MODE.VALUE_VALIDATE_MODE){
                        _current--;
                        AddNumericOrBool(dict, key, ParseValiable(terminateVariable));
                        mode = PARSE_MODE.KEY_VALIDATE_MODE;
                    }
                }
            }
            return dict;
        }

        void AddNumericOrBool(Dictionary<string, object> dict, string key, string val){
            dict.Add(key, GetValiable(val));
        }
        void AddNumericOrBool(List<object> list, string val)
        {
            list.Add(GetValiable(val));
        }

        object GetValiable(string val)
        {
            if (val == JSON_VARIABLE.TRUEFLG){
                return true;
            }
            else if (val == JSON_VARIABLE.FALSEFLG){
                return false;
            }
            else if (val == JSON_VARIABLE.NULL){
                return null;
            }
            else{
                if (val.IndexOf(".") != -1){
                    return Convert.ToInt64(val);
                }
                else{
                    return Convert.ToDouble(val);
                }
            }
        }

        private void Skip(char c)
        {
            while (_current < _source.Length){
                if (ReadCharacter() == c){
                    break;
                }
            }
        }

        private string ParseValiable(IsTerminate terminate)
        {
            StringBuilder str = new StringBuilder();
            while (_current < _source.Length){

                char c = ReadCharacter();
                if (terminate(c)) break;
                str.Append(c);
            }
            return str.ToString();
        }

        bool IsStringTerminate(char c)
        {
            return (c == JSON_SPECIAL_CODE.DOUBLE_QUOTE.GetHashCode() && _source[_current - 1] != '¥' && _source[_current - 2] != '¥');
        }

        bool IsVariableTerminate(char c)
        {
            if (c == JSON_SPECIAL_CODE.COMMA.GetHashCode() ||
                c == JSON_SPECIAL_CODE.SPACE.GetHashCode() || c == JSON_SPECIAL_CODE.CR.GetHashCode()){
                return true;
            }
            else if (c == JSON_SPECIAL_CODE.RIGHT_BRACE.GetHashCode() || c == JSON_SPECIAL_CODE.ARRAY_END.GetHashCode()){
                _current--;
                return true;
            }
            return false;
        }

        private List<object>  ParseArray()
        {
            List<object> list = new List<object>();

            IsTerminate terminateString = IsStringTerminate;
            IsTerminate terminateVariable = IsVariableTerminate;

            while (_current < _source.Length){
                char c = ReadCharacter();

                if(c == JSON_SPECIAL_CODE.DOUBLE_QUOTE.GetHashCode()){
                    list.Add(ParseValiable(terminateString));

                }else if(c == JSON_SPECIAL_CODE.LEFT_BRACE.GetHashCode()){
                    list.Add( ParseObject() );

                }else if(c == JSON_SPECIAL_CODE.ARRAY_START.GetHashCode()){
                    list.Add( ParseArray() );

                }else if(c == JSON_SPECIAL_CODE.ARRAY_END.GetHashCode()){
                    break;

                }else if(c == JSON_SPECIAL_CODE.COMMA.GetHashCode() || 
                    c == JSON_SPECIAL_CODE.SPACE.GetHashCode() ||
                    c == JSON_SPECIAL_CODE.CR.GetHashCode() ||
                    c == JSON_SPECIAL_CODE.LF.GetHashCode()){

                }else{
                    _current--;
                    AddNumericOrBool(list, ParseValiable(terminateVariable));
                }
            }
            return list;
        }

        private char ReadCharacter(){
            if(_current < _source.Length){
                return Convert.ToChar(_source[_current++]);
            }else{
                return Convert.ToChar(0);
            }
        }

        string ConvertUTF(string s){
            return Regex.Replace(s, @"\\u([\dA-Fa-f]{4})",
                v => ((char)Convert.ToInt32(v.Groups[1].Value, 16)).ToString());
        }

        public string Serialize()
        {
            return SerializeDict((Dictionary<string, object>)_jsonDict[""]);
        }

        private string SerializeDict(Dictionary<string, object> dict)
        {
            _currentIndent++;

            StringBuilder json = new StringBuilder();
            json.Append(Convert.ToChar(JSON_SPECIAL_CODE.LEFT_BRACE));
            if (0 < dict.Count)
            {
                json.Append(System.Environment.NewLine);
            }
            int nIndent = 0;

            foreach (KeyValuePair<string, object> pair in dict)
            {
                nIndent = _currentIndent;
                while(0 < nIndent--){
                    json.Append(INDENT);
                }

                if (string.IsNullOrEmpty(pair.Key) == false)
                {
                    json.Append(Convert.ToChar(JSON_SPECIAL_CODE.DOUBLE_QUOTE));
                    json.Append(ConvertUTF(pair.Key));
                    json.Append(Convert.ToChar(JSON_SPECIAL_CODE.DOUBLE_QUOTE));
                    json.Append(Convert.ToChar(JSON_SPECIAL_CODE.SEPARATER));
                    json.Append(Convert.ToChar(JSON_SPECIAL_CODE.SPACE));
                }

                if (pair.Value.GetType() == typeof(Dictionary<string, object>)){
                    json.Append(SerializeDict((Dictionary<string, object>)pair.Value));
                }
                else if (pair.Value.GetType() == typeof(List<object>)){
                    json.Append(SerializeList((List<object>)pair.Value));
                }
                else if (pair.Value.GetType() == typeof(string)){
                    json.Append(Convert.ToChar(JSON_SPECIAL_CODE.DOUBLE_QUOTE));
                    json.Append(ConvertUTF(pair.Value.ToString()));
                    json.Append(Convert.ToChar(JSON_SPECIAL_CODE.DOUBLE_QUOTE));
                }else{
                    json.Append(pair.Value.ToString().ToLower() );
                }

                json.Append(Convert.ToChar(JSON_SPECIAL_CODE.COMMA));
                json.Append(System.Environment.NewLine);
            }

            nIndent = --_currentIndent;
            if (0 < dict.Count)
            {
                json.Remove(json.ToString().Length - (System.Environment.NewLine.Length + 1), 1);
                while (0 < nIndent--)
                {
                    json.Append(INDENT);
                } 
            }

            json.Append(Convert.ToChar(JSON_SPECIAL_CODE.RIGHT_BRACE));
            return json.ToString();
        }

        private string SerializeList(List<object> list)
        {
            _currentIndent++;
            StringBuilder json = new StringBuilder();

            json.Append(Convert.ToChar(JSON_SPECIAL_CODE.ARRAY_START));
            if (0 < list.Count)
            {
                json.Append(System.Environment.NewLine);
            }
            int nIndent = 0;

            foreach (object val in list)
            {
                nIndent = _currentIndent;
                while (0 < nIndent--){
                    json.Append(INDENT);
                }

                if (val.GetType() == typeof(Dictionary<string, object>)){
                    json.Append(SerializeDict((Dictionary<string, object>)val));
                }
                else if (val.GetType() == typeof(List<object>)){
                    json.Append(SerializeList((List<object>)val));

                }
                else if (val.GetType() == typeof(string)){
                    json.Append(Convert.ToChar(JSON_SPECIAL_CODE.DOUBLE_QUOTE));
                    json.Append(ConvertUTF(val.ToString()));
                    json.Append(Convert.ToChar(JSON_SPECIAL_CODE.DOUBLE_QUOTE));
                }
                else{
                    if (string.IsNullOrEmpty(val.ToString())){
                        json.Append(JSON_VARIABLE.NULL.ToString());
                    }
                    else{
                        json.Append(val.ToString().ToLower());
                    }
                }

                json.Append(Convert.ToChar(JSON_SPECIAL_CODE.COMMA));
                json.Append(System.Environment.NewLine);
            }

            nIndent = --_currentIndent;
            if (0 < list.Count)
            {
                json.Remove(json.ToString().Length - (System.Environment.NewLine.Length + 1), 1);
                while (0 < nIndent--)
                {
                    json.Append(INDENT);
                }
            }
            json.Append(Convert.ToChar(JSON_SPECIAL_CODE.ARRAY_END));

            return json.ToString();
        }
    }
}
