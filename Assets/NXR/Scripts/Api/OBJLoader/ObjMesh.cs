
using UnityEngine;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System;

namespace Nxr.Internal
{
    public class ObjMesh
    {
        public struct IndexInfo
        {
            public Vector3Int indexVec;
            public int index;
        }

        public struct Vector3Int
        {
            public Vector3Int(int ix, int iy, int iz)
            {
                x = ix;
                y = iy;
                z = iz;
            }
            public int x;
            public int y;
            public int z;
        }

        /// <summary>
        /// UV坐标列表
        /// </summary>
        private List<Vector2> uvArrayList;

        /// <summary>
        /// 法线列表
        /// </summary>
        private List<Vector3> normalArrayList;

        /// <summary>
        /// 顶点列表
        /// </summary>
        private List<Vector3> vertexArrayList;

        /// <summary>
        /// 面相关的顶点索引、UV索引列表、法线索引
        /// </summary>
        private List<Vector3Int> faceVertexUVNormal;

        /// <summary>
        /// UV坐标数组
        /// </summary>
        public Vector2[] UVArray;

        /// <summary>
        /// 法线数组
        /// </summary>
        public Vector3[] NormalArray;

        /// <summary>
        /// 顶点数组
        /// </summary>
        public Vector3[] VertexArray;

        /// <summary>
        /// 面数组
        /// </summary>
        public int[] TriangleArray;

        /// <summary>
        /// 构造函数	/// </summary>
        public ObjMesh()
        {
            //初始化列表
            uvArrayList = new List<Vector2>();
            normalArrayList = new List<Vector3>();
            vertexArrayList = new List<Vector3>();
            faceVertexUVNormal = new List<Vector3Int>();
        }


        /// <summary>
        /// 从一个文本化后的.obj文件中加载模型
        /// 格式 ：f v/vt/vn v/vt/vn v/vt/vn（f 顶点索引 / 纹理坐标索引 / 顶点法向量索引）
        /// </summary>
        public ObjMesh LoadFromObj(string objText)
        {
            uvArrayList.Clear();
            normalArrayList.Clear();
            vertexArrayList.Clear();
            faceVertexUVNormal.Clear();
            UVArray = null;
            TriangleArray = null;
            NormalArray = null;
            VertexArray = null;

            double startMS = new TimeSpan(DateTime.Now.Ticks).TotalMilliseconds;
            if (objText.Length <= 0)
                return null;

            //v这一行在3dsMax中导出的.obj文件
            //  前面是两个空格后面是一个空格
            objText = objText.Replace("  ", " ");

            //将文本化后的obj文件内容按行分割
            string[] allLines = objText.Split('\n');
            foreach (string line in allLines)
            {
                //将每一行按空格分割
                string[] chars = line.Split(' ');
                //根据第一个字符来判断数据的类型
                switch (chars[0])
                {
                    case "v":
                        //处理顶点
                        this.vertexArrayList.Add(new Vector3(
                            ConvertToFloat(chars[1]),
                            ConvertToFloat(chars[2]),
                            ConvertToFloat(chars[3]))
                        );
                        break;
                    case "vn":
                        //处理法线
                        this.normalArrayList.Add(new Vector3(
                            ConvertToFloat(chars[1]),
                            ConvertToFloat(chars[2]),
                            ConvertToFloat(chars[3]))
                        );
                        break;
                    case "vt":
                        //处理UV
                        this.uvArrayList.Add(new Vector2(
                            ConvertToFloat(chars[1]),
                            ConvertToFloat(chars[2]))
                        );
                        break;
                    case "f":
                        //处理面
                        GetTriangleList(chars);
                        break;
                }
            }
            //合并三角面
            Combine();
            Debug.Log("ObjMesh Finish=" + (new TimeSpan(DateTime.Now.Ticks).TotalMilliseconds - startMS) + "MS");
            return this;
        }

        private string GenerateKey(Vector3Int vector3)
        {
            return "key_" + (int)vector3.x + "_" + (int)vector3.y + "_" + (int)vector3.z;
        }

        /// <summary>
        /// 合并三角面
        /// </summary>
        private void Combine()
        {
            Dictionary<string, ArrayList> CacheDict = new Dictionary<string, ArrayList>();
            for (int i = 0, size = faceVertexUVNormal.Count; i < size; i++)
            {
                Vector3Int tmpVec = faceVertexUVNormal[i];
                string key = GenerateKey(tmpVec);

                IndexInfo mIndexInfo = new IndexInfo();
                mIndexInfo.index = i;
                mIndexInfo.indexVec = tmpVec;
                if (CacheDict.ContainsKey(key))
                {
                    CacheDict[key].Add(mIndexInfo);
                }
                else
                {
                    CacheDict[key] = new ArrayList();
                    CacheDict[key].Add(mIndexInfo);
                }
            }

            //使用一个字典来存储要合并的索引信息
            Dictionary<int, ArrayList> toCambineList = new Dictionary<int, ArrayList>();
            for (int i = 0, size = faceVertexUVNormal.Count; i < size; i++)
            {
                if (faceVertexUVNormal[i].x != 0 && faceVertexUVNormal[i].y != 0 && faceVertexUVNormal[i].z != 0)
                {
                    Vector3Int iTemp = faceVertexUVNormal[i];
                    //相同索引的列表
                    ArrayList SameIndexList = new ArrayList();
                    SameIndexList.Add(i);
                    string key = GenerateKey(iTemp);
                    if (CacheDict.ContainsKey(key))
                    {
                        ArrayList mIdxInfoList = CacheDict[key];
                        foreach (IndexInfo IndexTtem in mIdxInfoList)
                        {
                            int j = IndexTtem.index;
                            if (j != i)
                            {
                                SameIndexList.Add(j);
                                faceVertexUVNormal[j] = new Vector3Int(0, 0, 0);
                            }
                        }
                    }
                    //用一个索引来作为字典的键名，这样它可以代替对应列表内所有索引
                    toCambineList.Add(i, SameIndexList);
                }
            }

            //初始化各个数组
            this.VertexArray = new Vector3[toCambineList.Count];
            this.UVArray = new Vector2[toCambineList.Count];
            this.NormalArray = new Vector3[toCambineList.Count];
            this.TriangleArray = new int[faceVertexUVNormal.Count];

            //定义遍历字典的计数器
            int count = 0;

            //遍历词典
            foreach (KeyValuePair<int, ArrayList> IndexTtem in toCambineList)
            {
                //根据索引给面数组赋值
                foreach (int item in IndexTtem.Value)
                {
                    TriangleArray[item] = count;
                }

                //当前的顶点、UV、法线索引信息
                Vector3Int VectorTemp = faceVertexUVNormal[IndexTtem.Key];

                //给顶点数组赋值
                VertexArray[count] = vertexArrayList[VectorTemp.x - 1];

                //给UV数组赋值
                if (uvArrayList.Count > 0)
                {
                    Vector2 tVec = uvArrayList[VectorTemp.y - 1];
                    UVArray[count] = new Vector2(tVec.x, tVec.y);
                }

                //给法线数组赋值
                if (normalArrayList.Count > 0)
                {
                    NormalArray[count] = normalArrayList[VectorTemp.z - 1];
                }

                count++;
            }
        }

        /// <summary>
        /// 获取面列表.格式 ：f v/vt/vn v/vt/vn v/vt/vn（f 顶点索引 / 纹理坐标索引 / 顶点法向量索引）
        /// </summary>
        /// <param name="chars">Chars.</param>
        private void GetTriangleList(string[] chars)
        {
            // f 960/1058/1195 961/1059/1196 962/1060/1197
            // 顶点索引： 960/961/962
            // 纹理索引：1058/1059/1060
            // 法线索引：1195/1196/1197
            List<Vector3Int> indexVectorList = new List<Vector3Int>();

            for (int i = 1, size = chars.Length; i < size; ++i)
            {
                //将每一行按照空格分割后从第一个元素开始
                //按照/继续分割可依次获得顶点索引、UV索引和法线索引
                string[] indexs = chars[i].Split('/');
                if (indexs.Length < 3) continue;

                Vector3Int indexVector = new Vector3Int(0, 0, 0);
                //顶点索引
                indexVector.x = ConvertToInt(indexs[0]);
                //UV索引
                if (indexs.Length > 1)
                {
                    if (indexs[1] != "")
                        indexVector.y = ConvertToInt(indexs[1]);
                }
                //法线索引
                if (indexs.Length > 2)
                {
                    if (indexs[2] != "")
                        indexVector.z = ConvertToInt(indexs[2]);
                }

                //将索引向量加入列表中
                indexVectorList.Add(indexVector);
            }

            //这里需要研究研究
            for (int j = 1; j < indexVectorList.Count - 1; ++j)
            {
                //按照0,1,2这样的方式来组成面
                faceVertexUVNormal.Add(indexVectorList[0]);
                faceVertexUVNormal.Add(indexVectorList[j]);
                faceVertexUVNormal.Add(indexVectorList[j + 1]);
            }
        }

        /// <summary>
        /// 将一个字符串转换为浮点类型
        /// </summary>
        /// <param name="s">待转换的字符串</param>
        /// <returns></returns>
        private float ConvertToFloat(string s)
        {
            return FastFloatParse(s); //(float)System.Convert.ToDouble(s, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 将一个字符串转化为整型	/// </summary>
        /// <returns>待转换的字符串</returns>
        /// <param name="s"></param>
        private int ConvertToInt(string s)
        {
            return FastIntParse(s); //System.Convert.ToInt32(s, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Modified from https://codereview.stackexchange.com/a/76891. Faster than float.Parse
        /// </summary>
        public static float FastFloatParse(string input)
        {
            input = cleanString(input);
            if (input.Contains("e") || input.Contains("E"))
                return float.Parse(input, CultureInfo.InvariantCulture);

            float result = 0;
            int pos = 0;
            int len = input.Length;

            if (len == 0) return float.NaN;
            char c = input[0];
            float sign = 1;
            if (c == '-')
            {
                sign = -1;
                ++pos;
                if (pos >= len) return float.NaN;
            }

            while (true) // breaks inside on pos >= len or non-digit character
            {
                if (pos >= len) return sign * result;
                c = input[pos++];
                if (c < '0' || c > '9') break;
                result = (result * 10.0f) + (c - '0');
            }

            if (c != '.' && c != ',') return float.NaN;
            float exp = 0.1f;
            while (pos < len)
            {
                c = input[pos++];
                if (c < '0' || c > '9') return float.NaN;
                result += (c - '0') * exp;
                exp *= 0.1f;
            }
            return sign * result;
        }

        /// <summary>
        /// Modified from http://cc.davelozinski.com/c-sharp/fastest-way-to-convert-a-string-to-an-int. Faster than int.Parse
        /// </summary>
        public static int FastIntParse(string input)
        {
            input = cleanString(input);
            int result = 0;
            bool isNegative = (input[0] == '-');

            for (int i = (isNegative) ? 1 : 0; i < input.Length; i++)
            {
                result = result * 10 + (input[i] - '0');
            }
            return (isNegative) ? -result : result;
        }

        private static string cleanString(string newStr)
        {
            string tempStr = newStr.Replace((char)13, ' ');
            return tempStr.Replace((char)10, ' ').Trim();
        }
    }
}