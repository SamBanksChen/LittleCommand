using System;
using System.Text;
using System.Collections.Generic;

namespace LitCommand
{
    public enum VarType { Null, Boolean, Number, Text }
    public enum LCMode { Debug, Normal }
    public struct LitAgm
    {
        public string value;
        public VarType type;
        public LitAgm(string value, VarType type)
        {
            this.value = value;
            this.type = type;
        }
    }
    public delegate string LitFunction(List<LitAgm> LitAgms, out string outtext,out bool missagm);
    public class LitCmd
    {
        public LitCmd(LitLibUnsafe BaseLib,LCMode lcMode)
        {
            LCmode = lcMode;
            lib = BaseLib;
            lib.ForcedParse = ForcedParse;
            lib.LCvars = LCvars;
            lib.GetVariableActualValue = ToVal;
            lib.GetVariableValue = GetVal;
            lib.SetVariableValue = SetVal;
            fncs = new Dictionary<string, LitFunction>(BaseLib.LibFncs);
        }
        public void ResetMode(LCMode lcMode)
        {
            LCmode = lcMode;
        }
        
        public bool AddFunction(string functionKey,LitFunction function)
        {
            return Extendfncs.TryAdd(functionKey, function);
        }
        public void RemoveFunction(string functionKey)
        {
            Extendfncs.Remove(functionKey);
        }
        public const byte SyntaxVersion = 1;
        public const byte StableVersion = 1;
        public const byte PatchVersion = 0;
        public static string version { get { return SyntaxVersion.ToString() + "." + StableVersion.ToString() + "." + PatchVersion.ToString(); } }
        LCMode LCmode;
        LitLibUnsafe lib;
        Dictionary<string,LitFunction> fncs;
        Dictionary<string,LitFunction> Extendfncs = new Dictionary<string,LitFunction>();
        Dictionary<string,string> LCvars = new Dictionary<string, string>();
        string outt = "";
        string error = "";
        public void LibError(string ErrorInfo)
        {
            error += "LIB: " + ErrorInfo + Environment.NewLine;
        }
        /// <summary>
        /// 执行命令行，常见指令格式有
        /// <code>call function param param ...</code>
        /// <code>a = 1 + b</code>
        /// <code>a = call function param</code>
        /// </summary>
        /// <param name="Command">传递指令</param>
        /// <param name="Error">返回执行中记录的错误</param>
        /// <returns>返回需要输出的内容</returns>
        public string Excute(string Command,out string Error)
        {
            Error = "";
            if (LCmode == LCMode.Debug) return Fmtlist(ToList(Command));
            else if (LCmode == LCMode.Normal) 
            {
                RunLine(ToList(Command));
                Error = error;
                return outt;
            }
            return "Nothing";
        }
        private void RunLine(List<string> cl)
        {
            bool agm = false;
            int dvt;
            string val;
            int i = 0;
            bool b;
            bool iscmd = false;
            outt = "";
            error = "";
            if(cl.Count > 1)
            {
                if (cl[1] == "=")
                {
                    agm = true;
                    i = 2;
                }
                //Fnc
                dvt = FindCall(cl) + 1;
                if (dvt != 0)
                {
                    val = RunFunc(cl, dvt, out b);
                    if (agm)
                    {
                        SetVal(cl[0], val);
                    }
                    iscmd = true;
                }
                //Cal
                if (agm && dvt == 0)
                {
                    val = BracketsTree(cl, i);
                    if (val != "")
                    {
                        SetVal(cl[0], val);
                    }
                    iscmd = true;
                }
                if (!iscmd)
                {
                    error += "Nothing" + Environment.NewLine;
                }
            }
            else
            {
                error += "Nothing" + Environment.NewLine;
            }
            return;
        }
        private string BracketsTree(List<string> l,int start)
        {
            List<string> Operands = new List<string>();
            List<string> Bkt = new List<string>();
            List<int> PriorityOperators = new List<int>();
            int ag = 0;
            bool isbk = false;
            int i = start;
            string v;
            float FirstNum;
            float SecondNum;
            //Brackets Stack
            while(i < l.Count)
            {
                if(l[i] == "(")
                {
                    if(ag == 0)
                    {
                        isbk = true;
                    }
                    ag++;
                }
                else if(l[i] == ")")
                {
                    ag--;
                    if (ag == 0)
                    {
                        isbk = true;
                        v = BracketsTree(Bkt,0);
                        Operands.Add(v);
                        Bkt.Clear();
                    }
                }
                if(ag == 0)
                {
                    if (!isbk)
                    {
                        Operands.Add(l[i]);
                    }
                    else
                    {
                        isbk = false;
                    }
                    if(l[i] == "*" || l[i] == "/" || l[i] == "%")
                    {
                        PriorityOperators.Add(Operands.Count - 1);
                    }
                }
                else
                {
                    if (!isbk)
                    {
                        Bkt.Add(l[i]);
                    }
                    else
                    {
                        isbk = false;
                    }
                }
                i++;
            }
            if(ag != 0)
            {
                error += "Brackets error" + Environment.NewLine;
                return "0";
            }
            i = 0;
            while(0 < PriorityOperators.Count)
            {
                i = PriorityOperators[0];
                if(!Operate(Operands[i-1],out FirstNum) & !Operate(Operands[i+1],out SecondNum))
                {
                    v = Calculate(FirstNum,Operands[i],SecondNum).ToString();
                    Operands[i-1] = v;
                    Operands.RemoveRange(i,2);
                    PORemove(ref PriorityOperators);
                }
                else
                {
                    error += "Expression error" + Environment.NewLine;
                    return "0";
                }
            }
            while(2 < Operands.Count)
            {
                if(!Operate(Operands[0],out FirstNum) & !Operate(Operands[2],out SecondNum))
                {
                    v = Calculate(FirstNum,Operands[1],SecondNum).ToString();
                    Operands[0] = v;
                    Operands.RemoveRange(1,2);
                }
                else
                {
                    error += "Expression error" + Environment.NewLine;
                    return "0";
                }
            }
            return Operands[0];
        }
        private float Calculate(float FirstNum,string Operator,float SecondNum)
        {
            float v;
            switch(Operator)
            {
                case "+":
                    v = FirstNum + SecondNum;
                    break;
                case "-":
                    v = FirstNum - SecondNum;
                    break;
                case "*":
                    v = FirstNum * SecondNum;
                    break;
                case "/":
                    v = FirstNum / SecondNum;
                    break;
                case "%":
                    v = FirstNum % SecondNum;
                    break;
                default:
                    v = 0;
                    error += "Operator not found" + Environment.NewLine;
                    break;
            }
            return v;
        }
        private void PORemove(ref List<int> li)
        {
            int i = 0;
            li.RemoveAt(0);
            while(i < li.Count)
            {
                li[i] -= 2;
                i++;
            }
        }
        private bool Operate(string Op,out float Number)
        {
            if(Op == "+" || Op == "-" || Op == "*" || Op == "/")
            {
                Number = 0;
                return true;
            }
            else
            {
                float.TryParse(ToVal(Op,out _),out Number);
                return false;
            }
        }
        private string RunFunc(List<string> agms,int dvt,out bool missagm)
        {
            string ret = "";
            missagm = false;
            List<LitAgm> Agmvs = new List<LitAgm>();
            int i = dvt + 1;
            while(i < agms.Count)
            {
                LitAgm agmv;
                agmv.value = ToVal(agms[i],out agmv.type);
                Agmvs.Add(agmv);
                i++;
            }
            if (fncs.TryGetValue(agms[dvt], out LitFunction? value) || Extendfncs.TryGetValue(agms[dvt], out value))
            {
                ret = value(Agmvs,out var outtext,out missagm);
                outt += outtext;
            }
            else
            {
                ret = "null";
                error += "Function not found" + Environment.NewLine;
            }
            if (missagm)
            {
                error += "Miss arguments" + Environment.NewLine;
            }
            return ret;
        }
        private int FindCall(List<string> l)
        {
            int i = 0;
            while(i < l.Count)
            {
                if(l[i] == "call")
                {
                    return i;
                }
                i++;
            }
            return -1;
        }
        private float ForcedParse(string s)
        {
            if(!float.TryParse(s,out float t))
            {
                error += "Not a number" + Environment.NewLine;
            }
            return t;
        }
        private string ToVal(string Cvar,out VarType CVtype)
        {
            if(Cvar == "null")
            {
                CVtype = VarType.Null;
                return "null";
            }
            if(Cvar == "")
            {
                CVtype = VarType.Text;
                return "";
            }
            //Number
            if(float.TryParse(Cvar,out _))
            {
                CVtype = VarType.Number;
                return Cvar;
            }
            //bool
            if(Cvar == "true" || Cvar == "false")
            {
                CVtype = VarType.Boolean;
                return Cvar;
            }
            //string
            var s = Cvar.AsSpan();
            List<string> sl = new List<string>();
            int i = 0;
            StringBuilder sb = new StringBuilder();
            while(i < s.Length)
            {
                sl.Add(s[i].ToString());
                i++;
            }
            if (sl.Count > 0 && sl[0] == "\"")
            {
                sl.RemoveAt(0);
                sl.RemoveAt(sl.Count - 1);
                i = 0;
                while(i < sl.Count)
                {
                    sb.Append(sl[i]);
                    i++;
                }
                CVtype = VarType.Text;
                return sb.ToString();
            }
            //var
            return ToVal(GetVal(Cvar), out CVtype);
        }
        private string GetVal(string CVar)
        {
            if(LCvars.TryGetValue(CVar,out var val))
            {
                return val;
            }
            else
            {
                error += "Variable not found" + Environment.NewLine;
                return "null";
            }
        }
        private void SetVal(string CVar,string value)
        {
            if (CVar != "true" && CVar != "false" && CVar != "null" && CVar != "call" && !int.TryParse(CVar, out _))
            {
                if(LCvars.ContainsKey(CVar))
                {
                    LCvars[CVar] = value;
                }
                else
                {
                    LCvars.Add(CVar, value);
                }
            }
        }
        private List<string> ToList(string cmd)
        {
            var cl = cmd.AsSpan();
            int i = 0;
            char s;
            StringBuilder c = new StringBuilder();
            List<string> Linec = new List<string>();
            while (i < cl.Length)
            {
                s = cl[i];
                if (s == ' ')
                {
                    if (c.ToString() != "")
                    {
                        Linec.Add(c.ToString());
                        c.Clear();
                    }
                }
                else
                {
                    if(s == '\"')
                    {
                        c.Append(s);
                        i++;
                        s = cl[i];
                        while(i < cl.Length && s != '\"')
                        {
                            c.Append(s);
                            i++;
                            s = cl[i];
                        }
                        c.Append(s);
                    }
                    else
                    {
                        c.Append(s);
                    }
                }
                i++;
            }
            if (c.ToString() != "")
            {
                Linec.Add(c.ToString());
                c.Clear();
            }
            return Linec;
        }
        private string Fmtlist(List<string> l)
        {
            StringBuilder s = new StringBuilder("{" + l[0]);
            int i = 1;
            while (i < l.Count)
            {
                s.Append("," + l[i]);
                i++;
            }
            s.Append("}");
            return s.ToString();
        }

    }
    public abstract class LitLib
    {
        public Dictionary<string, LitFunction> LibFncs;
        public delegate float ForcedParseFnc(string s);
        public ForcedParseFnc ForcedParse;
    }
    public abstract class LitLibUnsafe : LitLib
    {
        public Dictionary<string, string> LCvars;
        public delegate string ToVal(string CVar, out VarType Ctype);
        public delegate string GetVal(string CVar);
        public delegate void SetVal(string CVar, string value);
        public ToVal GetVariableActualValue;
        public GetVal GetVariableValue;
        public SetVal SetVariableValue;
    }
    public class NativeLib : LitLibUnsafe
    {
        public NativeLib()
        {
            LibFncs = new Dictionary<string, LitFunction>
            {
                {"version",Version },
                {"format",Format },
                {"printf",Printf },
                {"print",Print },
                {"add",Add },
                {"sub",Sub },
                {"mul",Mul },
                {"div",Div },
                {"mod",Mod },
                {"typeof",TypeOf },
                {"VarsClear",ClearAllVars },
                {"VarsRemove",RemoveVars }
            };
        }
        public string Version(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            outtext = LitCmd.version;
            missagm = false;
            return "null";
        }
        public string Format(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            var sb = new StringBuilder("\"");
            foreach (var s in LitAgms)
            {
                sb.Append(s);
            }
            sb.Append('\"');
            outtext = "";
            missagm = false;
            return sb.ToString();
        }
        public string Printf(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            var sb = new StringBuilder();
            foreach (var s in LitAgms)
            {
                sb.Append(s.value);
            }
            outtext = sb.ToString();
            missagm = false;
            return "null";
        }
        public string Print(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            if (LitAgms.Count >= 1)
            {
                missagm = false;
                outtext = LitAgms[0].value;
            }
            else
            {
                missagm = true;
                outtext = "";
            }
            return "null";
        }
        public string Add(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            if (LitAgms.Count >= 2)
            {
                missagm = false;
                outtext = "";
                float n = ForcedParse(LitAgms[0].value) + ForcedParse(LitAgms[1].value);
                return n.ToString();
            }
            else
            {
                missagm = true;
                outtext = "";
                return "0";
            }
        }
        public string Sub(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            if (LitAgms.Count >= 2)
            {
                missagm = false;
                outtext = "";
                float n = ForcedParse(LitAgms[0].value) - ForcedParse(LitAgms[1].value);
                return n.ToString();
            }
            else
            {
                missagm = true;
                outtext = "";
                return "0";
            }
        }
        public string Mul(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            if (LitAgms.Count >= 2)
            {
                missagm = false;
                outtext = "";
                float n = ForcedParse(LitAgms[0].value) * ForcedParse(LitAgms[1].value);
                return n.ToString();
            }
            else
            {
                missagm = true;
                outtext = "";
                return "0";
            }
        }
        public string Div(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            if (LitAgms.Count >= 2)
            {
                missagm = false;
                outtext = "";
                float n = ForcedParse(LitAgms[0].value) / ForcedParse(LitAgms[1].value);
                return n.ToString();
            }
            else
            {
                missagm = true;
                outtext = "";
                return "0";
            }
        }
        public string Mod(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            if (LitAgms.Count >= 2)
            {
                missagm = false;
                outtext = "";
                float n = ForcedParse(LitAgms[0].value) % ForcedParse(LitAgms[1].value);
                return n.ToString();
            }
            else
            {
                missagm = true;
                outtext = "";
                return "0";
            }
        }
        public string TypeOf(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            if (LitAgms.Count >= 1)
            {
                missagm = false;
                outtext = LitAgms[0].type.ToString();
                return LitAgms[0].type.ToString();
            }
            else
            {
                missagm = true;
                outtext = "";
                return "null";
            }
        }
        public string ClearAllVars(List<LitAgm> LitAgms,out string outtext,out bool missagm)
        {
            LCvars.Clear();
            outtext = "All vars are cleared";
            missagm = false;
            return "null";
        }
        public string RemoveVars(List<LitAgm> LitAgms, out string outtext, out bool missagm)
        {
            if(LitAgms.Count >= 1)
            {
                missagm = false;
                outtext = "The Var " + LitAgms[0].value + " is Destroyed";
                LCvars.Remove(LitAgms[0].value);
                return "null";
            }
            else
            {
                missagm = true;
                outtext = "";
                return "null";
            }
        }
    }
}