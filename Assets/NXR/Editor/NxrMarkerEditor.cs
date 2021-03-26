using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Nxr.Internal
{
    public class NxrMarkerEditor
    {

        [DrawGizmo(GizmoType.InSelectionHierarchy)] // Draw the gizmo if it 
        static void RenderARTrackedObjectGizmo(NibiruMarker marker, GizmoType gizmoType)
        {
            bool selected = (gizmoType & GizmoType.Active) != 0;
            if (selected)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawCube(marker.transform.position - new Vector3(0, 0.02f, 0), new Vector3(0.08f, 0.001f, 0.08f));
                Gizmos.color = Color.red;
                DrawWord("Marker", 0.01f, new Vector3(-0.15f, -0.02f, -0.10f), new Vector3(0, 0, 1), new Vector3(1, 0, 0));
            }
        }

        private static void DrawRectangle(Vector3 centre, Vector3 up, Vector3 right, float width, float height, Color color)
        {

            Gizmos.color = color;

            //ARController.Log("DrawRectangle centre=" + centre.ToString("F3") + ", up=" + up.ToString("F3") + ", right=" + right.ToString("F3") + ", width=" + width.ToString("F3") + ", height=" + height.ToString("F3") + ".");
            Vector3 u = up * height;
            Vector3 r = right * width;
            Vector3 p = centre - (u * 0.5f) - (r * 0.5f);

            Gizmos.DrawLine(p, p + u);
            Gizmos.DrawLine(p + u, p + u + r);
            Gizmos.DrawLine(p + u + r, p + r);
            Gizmos.DrawLine(p + r, p);
        }

        private static void DrawWord(String word, float size, Vector3 origin, Vector3 forward, Vector3 right)
        {
            foreach (char c in word.ToUpper())
            {
                DrawLetter(c, size, origin, forward, right);
                origin += right * size * 6.0f;
            }
        }

        private static void DrawLetter(char letter, float size, Vector3 origin, Vector3 forward, Vector3 right)
        {
            String path = letters[letter];

            Vector3 f = forward * size;
            Vector3 r = right * size;

            Vector3 down = origin;
            Vector3 current = origin;

            foreach (char c in path)
            {
                switch (c)
                {
                    case '(':
                        down = current;
                        continue;
                    case ')':
                        Gizmos.DrawLine(down, current);
                        continue;
                    case 'U':
                        current += f;
                        break;
                    case 'D':
                        current -= f;
                        break;
                    case 'R':
                        current += r;
                        break;
                    case 'L':
                        current -= r;
                        break;
                }
            }
        }

        private readonly static Dictionary<char, String> letters = new Dictionary<char, String>() {
        {' ', ""},
        {'!', "RR(U)U(UU)"},
        {'"', "UUUUR(D)URR(D)"},
        {'#', "R(UUUU)RR(DDDD)UR(LLLL)UU(RRRR)"},
        {'$', "RR(UUUU)DR(LL)(D)(RR)(D)(LL)"},
        {'%', "RUUUU(D)DDD(RRUUUU)DDD(D)"},
        {'&', "RRRRUU(DDLL)(L)(UL)(RRRUU)(UL)(DL)(DDDRRR)"},
        {'\'',"RRUUU(U)"},
        {'(', "RRR(UL)(UU)(UR)"},
        {')', "R(UR)(UU)(UL)"},
        {'*', "RR(UUUU)DL(DDRR)UU(DDLL)UL(RRRR)"},
        {'+', "RUU(RR)LU(DD)"},
        {',', "R(UR)"},
        {'-', "UR(RR)"},
        {'.', "RR(U)"},
        {'/', "R(UUUURR)"},
        {'0', "(UUUU)(RRRR)(DDDD)(LLLL)"},
        {'1', "RR(UUUU)"},
        {'2', "UUU(UR)(RR)(DR)(DDDLLLL)(RRRR)"},
        {'3', "U(DR)(RR)(UR)(ULL)(URR)(UL)(LL)(DL)"},
        {'4', "RRRRU(LLLL)(UUURRR)(DDDD)"},
        {'5', "(RRRR)(UU)(LLLL)(UU)(RRRR)"},
        {'6', "UUUURRR(LL)(DDL)(DD)(RRRR)(UU)(LLLL)"},
        {'7', "UUUU(RRRR)(DDDDLL)"},
        {'8', "(UUUU)(RRRR)(DDDD)(LLLL)UU(RRRR)"},
        {'9', "R(RR)(UUR)(LLLL)(UU)(RRRR)(DD)"},
        {':', "RR(U)UU(U)"},
        {';', "R(UR)UU(U)"},
        {'<', "RRR(UULL)(RRUU)"},
        {'=', "UR(RR)UU(LL)"},
        {'>', "R(UURR)(LLUU)"},
        {'?', "RR(U)U(R)(UR)(UL)(L)(DL)"},
        {'@', "RRR(LL)(UL)(UU)(UR)(RR)(DR)(DD)(LL)(U)(R)(D)"},
        {'A', "(UUU)(UR)(RR)(DR)(DDD)UU(LLLL)"},
        {'B', "(UUUU)(RRR)(DR)(DD)(DL)(LLL)UU(RRRR)"},
        {'C', "RRRRU(DL)(LL)(UL)(UU)(UR)(RR)(DR)"},
        {'D', "(UUUU)(RRR)(DR)(DD)(DL)(LLL)"},
        {'E', "(UUUU)(RRRR)DDLL(LL)DD(RRRR)"},
        {'F', "(UUUU)(RRRR)DDLL(LL)"},
        {'G', "UURR(RR)(DD)(LLL)(UL)(UU)(UR)(RR)(DR)"},
        {'H', "(UUUU)DD(RRRR)UU(DDDD)"},
        {'I', "(RRRR)LL(UUUU)LL(RRRR)"},
        {'J', "U(D)(RR)(UUUU)LL(RRRR)"},
        {'K', "(UUUU)RRRR(DDLLLL)(DDRRRR)"},
        {'L', "UUUU(DDDD)(RRRR)"},
        {'M', "(UUUU)(DDRR)(UURR)(DDDD)"},
        {'N', "(UUUU)(DDDDRRRR)(UUUU)"},
        {'O', "U(UU)(UR)(RR)(DR)(DD)(DL)(LL)(UL)"},
        {'P', "(UUUU)(RRR)(DR)(D)(DL)(LLL)"},
        {'Q', "U(UU)(UR)(RR)(DR)(DD)(DL)(LL)(UL)RRR(DR)"},
        {'R', "(UUUU)(RRR)(DR)(D)(DL)(LLL)RRR(DR)"},
        {'S', "U(DR)(RR)(UR)(UL)(LL)(UL)(UR)(RR)(DR)"},
        {'T', "RR(UUUU)LL(RRRR)"},
        {'U', "UUUU(DDDD)(RRR)(UR)(UUU)"},
        {'V', "UUUU(DDDDRR)(UUUURR)"},
        {'W', "UUUU(DDDDR)(UUR)(DDR)(UUUUR)"},
        {'X', "(UUUURRRR)LLLL(DDDDRRRR)"},
        {'Y', "UUUU(DDRR)(UURR)DDLL(DD)"},
        {'Z', "UUUU(RRRR)(DDDDLLLL)(RRRR)"},
        {'[', "RRR(L)(UUUU)(R)"},
        {'\\',"RRR(UUUULL)"},
        {']', "R(R)(UUUU)(L)"},
        {'^', "UUR(UUR)(DDR)"},
        {'_', "(RRRR)"},
        {'`', "UUUURR(DR)"},
        {'{', "RRR(L)(U)(LU)(RU)(U)(R)"},
        {'|', "RR(UUUU)"},
        {'}', "R(R)(U)(RU)(LU)(U)(L)"},
        {'~', "UU(UR)(DDRR)(UR)"}
    };



    }
}
