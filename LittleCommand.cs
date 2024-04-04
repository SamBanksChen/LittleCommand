using System;
using System.Text;
using System.Collections.Generic;

namespace LitCommand
{
	public struct LCvar
	{
		public string name;
		public string value;
	}
	public class LitCmd
	{
		public LitCmd(int Mode = 1)
		{
			LCMode = Mode;
		}
		public const int Mode_Debug = 0;
		public const int Mode_Normal = 1;
		//public const int Mode_Analysis = 2;
		//public const int Mode_Program_Debug = 3;
		//public const int Mode_Program_Release = 4;
		
		public void SetMode(int Mode)
		{
			LCMode = Mode;
		}
		/// <summary>
		/// 设置拓展函数，设置null取消拓展
		/// </summary>
		/// <param name="extendFNC">Function or null</param>
		public void SetExtendFNC(ExtendFNC extendFNC)
        {
			this.extendFNC = extendFNC;
        }
		public const string version = "0.1.3";
		public delegate string ExtendFNC(string FNCname, List<Param> FNCparams, out string outtext);
		ExtendFNC extendFNC = null;
		int LCMode;
		List<LCvar> LCvars = new List<LCvar>();
		string outt = "";
		string error = "";
		/// <summary>
		/// 执行命令行，常见指令格式有
		/// <code>call function param param ...</code>
		/// <code>a = 1 + b</code>
		/// <code>a = call function param</code>
		/// </summary>
		/// <param name="Command"></param>
		/// <returns></returns>
		public string Excute(string Command)
		{
			string ot;
			if (LCMode == 0) return fmtlist(ToList(Command));
			else if (LCMode == 1) 
			{
				RunLine(ToList(Command));
				ot = outt;
				if(error != "")
                {
					ot += error;
				}
				return ot;
			}
			//else if (LCMode == 2) return "";
			else return "Mode error";
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
			int i = 1;
			int t;
			string n;
			List<string> agmvs = new List<string>();
			missagm = false;
			switch(agms[dvt])
			{
				case "version":
					outt += version;
					break;
				case "format":
					StringBuilder rsb = new StringBuilder("\"");
					while (i < agms.Count - dvt)
					{
						rsb.Append(ToVal(agms[dvt + i],out n));
						i++;
					}
					rsb.Append("\"");
					ret = rsb.ToString();
					break;
				case "printf":
					rsb = new StringBuilder("\"");
					while (i < agms.Count - dvt)
					{
						rsb.Append(ToVal(agms[dvt + i], out n));
						i++;
					}
					rsb.Append("\"");
					outt += rsb.ToString();
					break;
				case "print":
					if(agms.Count - dvt > 1)
					{
						outt += ToVal(agms[dvt + 1],out n) + Environment.NewLine;
					}
                    else
                    {
						missagm = true;
                    }
					break;
				case "add":
					if(agms.Count - dvt > 2)
					{
						agmvs.Add(ToVal(agms[dvt + 1],out n));
						agmvs.Add(ToVal(agms[dvt + 2],out n));
						t = ForcedParse(agmvs[0]) + ForcedParse(agmvs[1]);
						ret = t.ToString();
					}
					else
					{
						missagm = true;
					}
					break;
				case "sub":
					if(agms.Count - dvt > 2)
					{
						agmvs.Add(ToVal(agms[dvt + 1],out n));
						agmvs.Add(ToVal(agms[dvt + 2],out n));
						t = ForcedParse(agmvs[0]) - ForcedParse(agmvs[1]);
						ret = t.ToString();
					}
					else
					{
						missagm = true;
					}
					break;
				case "mul":
					if(agms.Count - dvt > 2)
					{
						agmvs.Add(ToVal(agms[dvt + 1], out n));
                        agmvs.Add(ToVal(agms[dvt + 2], out n));
						t = ForcedParse(agmvs[0]) * ForcedParse(agmvs[1]);
						ret = t.ToString();
					}
					else
					{
						missagm = true;
					}
					break;
				case "div":
					if(agms.Count - dvt > 2)
					{
						agmvs.Add(ToVal(agms[dvt + 1], out n));
                        agmvs.Add(ToVal(agms[dvt + 2], out n));
						t = ForcedParse(agmvs[0]) / ForcedParse(agmvs[1]);
						ret = t.ToString();
					}
					else
					{
						missagm = true;
					}
					break;
				case "mod":
					if(agms.Count - dvt > 2)
					{
						agmvs.Add(ToVal(agms[dvt + 1], out n));
                        agmvs.Add(ToVal(agms[dvt + 2], out n));
						t = ForcedParse(agmvs[0]) % ForcedParse(agmvs[1]);
						ret = t.ToString();
					}
					else
					{
						missagm = true;
					}
					break;
				case "typeof":
					if(agms.Count - dvt > 1)
                    {
						ToVal(agms[dvt + 1], out n);
						outt += n;
                    }
                    else
                    {
						missagm = true;
                    }
					break;
				//case
				//More Function
				default:
					if(extendFNC != null)
                    {
						List<Param> ps = new List<Param>();
						i = 1;
						while (i < agms.Count - dvt)
						{
							Param pm;
							pm.value = ToVal(agms[dvt + i], out n);
							pm.type = n;
							ps.Add(pm);
							i++;
						}
						string outinfo;
						ret = extendFNC(agms[dvt], ps, out outinfo);
						outt += outinfo;
					}
                    else 
					{
						ret = "";
						error += "Function not found" + Environment.NewLine;
					}
					break;
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
		private int ForcedParse(string s)
		{
			int t;
			int.TryParse(s,out t);
			return t;
		}
		private string ToVal(string Cvar,out string CVtype)
		{
			if(Cvar == "")
            {
				CVtype = "null";
				return "";
            }
			float n;
			//Number
			if(float.TryParse(Cvar,out n))
			{
				CVtype = "float";
				return Cvar;
			}
			//bool
			if(Cvar == "true" || Cvar == "false")
			{
				CVtype = "bool";
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
			if (sl.Count > 0 && sl[0].ToString() == "\"")
			{
				sl.RemoveAt(0);
				sl.RemoveAt(sl.Count - 1);
				i = 0;
				while(i < sl.Count)
				{
					sb.Append(sl[i]);
					i++;
				}
				CVtype = "string";
				return sb.ToString();
			}
			//var
			return ToVal(GetVal(Cvar), out CVtype);
		}
		private string GetVal(string CVar)
		{
			int i = 0;
			while(i < LCvars.Count)
			{
				if(CVar == LCvars[i].name)
				{
					return LCvars[i].value;
				}
				i++;
			}
			error += "Variable not found" + Environment.NewLine;
			return "";
		}
		private void SetVal(string CVar,string value)
		{
			int n;
			if(CVar != "true" & CVar != "false" & !int.TryParse(CVar, out n))
			{
				int i = FindVar(CVar);
				if(i != -1)
				{
					LCvar v = LCvars[i];
					v.value = value;
					LCvars[i] = v;
				}
				else
				{
					CreateVar(CVar,value);
				}
			}
		}
		private int FindVar(string CVar)
		{
			int i = 0;
			while(i < LCvars.Count)
			{
				if(CVar == LCvars[i].name)
				{
					return i;
				}
				i++;
			}
			return -1;
		}
		private void CreateVar(string Varname,string Varvalue)
		{
			LCvar v;
			v.name = Varname;
			v.value = Varvalue;
			LCvars.Add(v);
		}
		private List<string> ToList(string cmd)
		{
			var cl = cmd.AsSpan();
			int i = 0;
			string s;
			StringBuilder c = new StringBuilder();
			List<string> Linec = new List<string>();
			while (i < cl.Length)
			{
				s = cl.Slice(i, 1).ToString();
				if (s == " ")
				{
					if (c.ToString() != "")
					{
						Linec.Add(c.ToString());
						c.Clear();
					}
				}
				else
				{
					c.Append(s);
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
		private string fmtlist(List<string> l)
		{
			StringBuilder s = new StringBuilder(l[0] + ":");
			int i = 1;
			while (i < l.Count)
			{
				s.Append("\n --- " + l[i]);
				i++;
			}
			return s.ToString();
		}
	}
	public struct Param
    {
		public string value;
		public string type;
    }
}