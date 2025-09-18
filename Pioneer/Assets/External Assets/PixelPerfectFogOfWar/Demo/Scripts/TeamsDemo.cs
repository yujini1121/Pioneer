using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FOW.Demos
{
    public class TeamsDemo : MonoBehaviour
    {
        public Text teamText;

        public Color team1Color = Color.blue;
        public List<FogOfWarRevealer> team1Members = new List<FogOfWarRevealer>();
        public Color team2Color = Color.green;
        public List<FogOfWarRevealer> team2Members = new List<FogOfWarRevealer>();
        public Color team3Color = Color.red;
        public List<FogOfWarRevealer> team3Members = new List<FogOfWarRevealer>();

        int team;
        private void Awake()
        {
            team = 2;
            changeTeams();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                changeTeams();
            }
        }

        void changeTeams()
        {
            team++;
            team = team % 3;

            teamText.text = $"VIEWING AS TEAM {team+1}";

            foreach (FogOfWarRevealer rev in team1Members)
            {
                rev.enabled = false;
                rev.GetComponent<FogOfWarHider>().enabled = true;
            }
            foreach (FogOfWarRevealer rev in team2Members)
            {
                rev.enabled = false;
                rev.GetComponent<FogOfWarHider>().enabled = true;
            }
            foreach (FogOfWarRevealer rev in team3Members)
            {
                rev.enabled = false;
                rev.GetComponent<FogOfWarHider>().enabled = true;
            }
            switch (team)
            {
                case 0:
                    teamText.color = team1Color;
                    foreach (FogOfWarRevealer rev in team1Members)
                    {
                        rev.enabled = true;
                        rev.GetComponent<FogOfWarHider>().enabled = false;
                    }
                    break;
                case 1:
                    teamText.color = team2Color;
                    foreach (FogOfWarRevealer rev in team2Members)
                    {
                        rev.enabled = true;
                        rev.GetComponent<FogOfWarHider>().enabled = false;
                    }
                    break;
                case 2:
                    teamText.color = team3Color;
                    foreach (FogOfWarRevealer rev in team3Members)
                    {
                        rev.enabled = true;
                        rev.GetComponent<FogOfWarHider>().enabled = false;
                    }
                    break;
            }
        }
    }
}
