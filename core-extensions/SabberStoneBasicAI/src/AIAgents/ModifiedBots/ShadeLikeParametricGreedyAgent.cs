/*
TODO
1. nowe parametry
2. wybór zestawu -> arbitralnie lub z parametrami
3. odpalanie pythona i porównanie wag

 */

using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;
using SabberStoneBasicAI.AIAgents;
using SabberStoneBasicAI.PartialObservation;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStoneBasicAI.AIAgents
{
	class ShadeLikeParametricGreedyAgent : AbstractAgent
	{
		public override void FinalizeAgent()
		{
			
		}

		public override void FinalizeGame()
		{
			
		}

		public static int NUM_PARAMETERS = 21;
		public static string HERO_HEALTH_REDUCED = "HERO_HEALTH_REDUCED";
		public static string HERO_ATTACK_REDUCED = "HERO_ATTACK_REDUCED";
		public static string MINION_HEALTH_REDUCED = "MINION_HEALTH_REDUCED";
		public static string MINION_ATTACK_REDUCED = "MINION_ATTACK_REDUCED";
		public static string MINION_KILLED = "MINION_KILLED";
		public static string MINION_APPEARED = "MINION_APPEARED";
		public static string SECRET_REMOVED = "SECRET_REMOVED";
		public static string MANA_REDUCED = "MANA_REDUCED";

		public static string M_HEALTH = "M_HEALTH";
		public static string M_ATTACK = "M_ATTACK";
		//public static string M_HAS_BATTLECRY = "M_HAS_BATTLECRY";
		public static string M_HAS_CHARGE = "M_HAS_CHARGE";
		public static string M_HAS_DEAHTRATTLE = "M_HAS_DEAHTRATTLE";
		public static string M_HAS_DIVINE_SHIELD = "M_HAS_DIVINE_SHIELD";
		public static string M_HAS_INSPIRE = "M_HAS_INSPIRE";
		public static string M_HAS_LIFE_STEAL = "M_HAS_LIFE_STEAL";
		public static string M_HAS_STEALTH = "M_HAS_STEALTH";
		public static string M_HAS_TAUNT = "M_HAS_TAUNT";
		public static string M_HAS_WINDFURY = "M_HAS_WINDFURY";
		public static string M_RARITY = "M_RARITY";
		public static string M_MANA_COST = "M_MANA_COST";
		public static string M_POISONOUS = "M_POISONOUS";		

		public Dictionary<string, double> weights;


		public override PlayerTask GetMove(POGame poGame)
		{
			
			debug("CURRENT TURN: " + poGame.Turn);
			KeyValuePair<PlayerTask,double> p = getBestTask(poGame);
			debug("SELECTED TASK TO EXECUTE "+stringTask(p.Key)+ "HAS A SCORE OF "+p.Value);
			
			debug("-------------------------------------");
			//Console.ReadKey();

			return p.Key;
		}

		//Mejor hacer esto con todas las posibles en cada movimiento
		public double scoreTask(POGame before, POGame after) {
			if (after == null) { //There was an exception with the simullation function!
				return 1; //better than END_TURN, just in case
			}
			
				if (after.CurrentOpponent.Hero.Health <= 0)
				{
					debug("KILLING ENEMY!!!!!!!!");
					return Int32.MaxValue;
				}
				if (after.CurrentPlayer.Hero.Health <= 0)
				{
					debug("WARNING: KILLING MYSELF!!!!!");
					return Int32.MinValue;
				}
			

			//Differences in Health
			debug("CALCULATING ENEMY HEALTH SCORE");
			double enemyPoints = calculateScoreHero(before.CurrentOpponent,after.CurrentOpponent);
			debug("CALCULATING MY HEALTH SCORE");
			double myPoints    = calculateScoreHero(before.CurrentPlayer, after.CurrentPlayer);
			debug("Enemy points: " + enemyPoints + " My points: " + myPoints);

			//Differences in Minions
			debug("CALCULATING ENEMY MINIONS");			
			double scoreEnemyMinions = calculateScoreMinions(before.CurrentOpponent.BoardZone,after.CurrentOpponent.BoardZone);
			debug("Score enemy minions: " +scoreEnemyMinions);
			debug("CALCULATING MY MINIONS");
			double scoreMyMinions = calculateScoreMinions(before.CurrentPlayer.BoardZone, after.CurrentPlayer.BoardZone);
			debug("Score my minions: " + scoreMyMinions);

			//Differences in Secrets
			debug("CALCULATING SECRETS");			
			double scoreEnemySecrets = calculateScoreSecretsRemoved(before.CurrentOpponent, after.CurrentOpponent);
			double scoreMySecrets    = calculateScoreSecretsRemoved(before.CurrentPlayer, after.CurrentPlayer);


			//Difference in Mana
			int usedMana = before.CurrentPlayer.RemainingMana - after.CurrentPlayer.RemainingMana;
			double scoreManaUsed = usedMana * weights[MANA_REDUCED];
			debug("Final task score" + enemyPoints + ",neg("+ myPoints + ")," + scoreEnemyMinions + ",neg(" + scoreMyMinions+"),S:"+ scoreEnemySecrets+"neg( " +scoreMySecrets+ ") M:neg(:"+scoreManaUsed+")");			
			return enemyPoints - myPoints + scoreEnemyMinions - scoreMyMinions+ scoreEnemySecrets - scoreMySecrets - scoreManaUsed;
		}

		double calculateScoreHero(Controller playerBefore, Controller playerAfter) {
			debug(playerBefore.Hero.Health + "("+playerBefore.Hero.Armor+")/"+playerBefore.Hero.AttackDamage+" --> "+
				 playerAfter.Hero.Health + "(" + playerAfter.Hero.Armor + ")/" + playerAfter.Hero.AttackDamage
				);
			int diffHealth = (playerBefore.Hero.Health + playerBefore.Hero.Armor) - (playerAfter.Hero.Health+playerAfter.Hero.Armor);
			int diffAttack = (playerBefore.Hero.AttackDamage) - (playerAfter.Hero.AttackDamage);
			//debug("DIFS"+diffHealth + " " + diffAttack);
			double score = diffHealth * weights[HERO_HEALTH_REDUCED] + diffAttack * weights[HERO_ATTACK_REDUCED];
			return score;
		}

		double calculateScoreMinions(SabberStoneCore.Model.Zones.BoardZone before, SabberStoneCore.Model.Zones.BoardZone after) {
			foreach (Minion m in before.GetAll())
			{
				debug("BEFORE "+stringMinion(m));
			}

			foreach (Minion m in after.GetAll())
			{
				debug("AFTER  " +stringMinion(m));
			}


			double scoreHealthReduced = 0;
			double scoreAttackReduced = 0; //We should add Divine shield removed?
			double scoreKilled = 0;
			double scoreAppeared = 0;

			//Minions modified?
			foreach (Minion mb in before.GetAll())
			{
				bool survived = false;
				foreach (Minion ma in after.GetAll())
				{
					if (ma.Id == mb.Id)
					{
						scoreHealthReduced = scoreHealthReduced + weights[MINION_HEALTH_REDUCED]*(mb.Health - ma.Health)*scoreMinion(mb); //Positive points if health is reduced
						scoreAttackReduced = scoreAttackReduced + weights[MINION_ATTACK_REDUCED]*(mb.AttackDamage - ma.AttackDamage)*scoreMinion(mb); //Positive points if attack is reduced
						survived = true;
						
					}
				}

				if (survived == false)
				{
					debug(stringMinion(mb) + " was killed");
					scoreKilled = scoreKilled + scoreMinion(mb)*weights[MINION_KILLED]; //WHATEVER //Positive points if card is dead
				}

			}

			//New Minions on play?
			foreach (Minion ma in after.GetAll())
			{
				bool existed = false;
				foreach (Minion mb in before.GetAll())
				{
					if (ma.Id == mb.Id)
					{
						existed = true;
					}
				}
				if (existed == false) {
					debug(stringMinion(ma)+ " is NEW!!");
					scoreAppeared = scoreAppeared + scoreMinion(ma)*weights[MINION_APPEARED]; //Negative if a minion appeared (below)
				}
			}

			//Think always as positive points if the enemy suffers!
			return scoreHealthReduced+scoreAttackReduced+scoreKilled-scoreAppeared; //CHANGE THESE SIGNS ACCORDINGLY!!!

		}

		double calculateScoreSecretsRemoved(Controller playerBefore, Controller playerAfter) {

			int dif = playerBefore.SecretZone.Count - playerAfter.SecretZone.Count;
			/*if (dif != 0) {
				Console.WriteLine("STOP");
			}*/
			//int dif = playerBefore.NumSecretsPlayedThisGame - playerAfter.NumSecretsPlayedThisGame;
			return dif * weights[SECRET_REMOVED];
		}

		double scoreMinion(Minion m) {
			//return 1;

			double score = m.Health*weights[M_HEALTH] + m.AttackDamage*weights[M_ATTACK];
			/*if (m.HasBattleCry)
				score = score + weights[M_HAS_BATTLECRY];*/
			if (m.HasCharge)
				score = score + weights[M_HAS_CHARGE];
			if (m.HasDeathrattle)
				score = score + weights[M_HAS_DEAHTRATTLE];
			if (m.HasDivineShield) 
				score = score + weights[M_HAS_DIVINE_SHIELD];
			if (m.HasInspire)
				score = score + weights[M_HAS_INSPIRE];
			if (m.HasLifeSteal)
				score = score + weights[M_HAS_LIFE_STEAL];
			if (m.HasTaunt)
				score = score + weights[M_HAS_TAUNT];
			if (m.HasWindfury)
				score = score + weights[M_HAS_WINDFURY];

			

			score = score + m.Card.Cost*weights[M_MANA_COST];
			score = score + rarityToInt(m.Card) * weights[M_RARITY];
			if (m.Poisonous) {
				score = score + weights[M_POISONOUS];
			}
			return score;

		}

		public int rarityToInt(SabberStoneCore.Model.Card c) {
			if (c.Rarity == SabberStoneCore.Enums.Rarity.COMMON)
			{
				return 1;
			}
			if (c.Rarity == SabberStoneCore.Enums.Rarity.FREE)
			{
				return 1;
			}
			if (c.Rarity == SabberStoneCore.Enums.Rarity.RARE)
			{
				return 2;
			}
			if (c.Rarity == SabberStoneCore.Enums.Rarity.EPIC)
			{
				return 3;
			}
			if (c.Rarity == SabberStoneCore.Enums.Rarity.LEGENDARY)
			{
				return 4;
			}
			return 0;
		}

		KeyValuePair<PlayerTask, double> getBestTask(POGame state) {
			double bestScore = Double.MinValue;
			PlayerTask bestTask = null;
			List<PlayerTask> list  = state.CurrentPlayer.Options();
			foreach (PlayerTask t in list) {
				debug("---->POSSIBLE "+stringTask(t));
				
				double score = 0;
				POGame before = state;
				if (t.PlayerTaskType == PlayerTaskType.END_TURN)
				{
					score = 0;
				}
				else
				{
					List<PlayerTask> toSimulate = new List<PlayerTask>();
					toSimulate.Add(t);
					Dictionary<PlayerTask, POGame> simulated = state.Simulate(toSimulate);
					//Console.WriteLine("SIMULATION COMPLETE");
					POGame nextState = simulated[t];
					score = scoreTask(state, nextState); //Warning: if using tree, avoid overflow with max values!
					
					
				}
				debug("SCORE " + score);
				if (score >= bestScore)
				{
					bestTask = t;
					bestScore = score;
				}

			}

			return new KeyValuePair<PlayerTask, double>(bestTask,bestScore);
		}

		public override void InitializeAgent()
		{
			debug("INITIALIZING AGENT (ONLY ONCE)");
			// mutants only
			// setAgeintWeightsFromString("0.79772286#0.64642786#0.3633397#0.78405817#0.53132201#0.70726014#0.37291524#0.20189696#0.07014928#0.78355544#0.69463728#0.58370084#0.91649906#0.474657#0.67174753#0.16183945#0.98521992#0.43340778#0.56122326#0.72634069#0.92028133");
			// mutants vs parents
			// setAgeintWeightsFromString("0.29200656#0.98765522#0.02041276#0.63498946#0.74946424#0.94758804#0.47603056#0.05988198#0.40201173#0.1345972#0.29181048#0.18548338#0.38815854#0.27825873#0.83269858#0.61753036#0.0690502#0.25331931#0.91811351#0.57604443#0.15545563");
			// researchers
			// setAgeintWeightsFromString("0.338317197113748#0.934667131789177#0.0931761926521559#0#0.751099024630008#1#0.961847886337875#0#1#0.552937948005278#0.59671345821399#0#1#0#0#1#0#0.00214012656947975#0.371668433465342#0.169469073202283#0");
			// pure shade
			// setAgeintWeightsFromString("0.409175#0.526988#0.077657#0.004964#0.619939#0.724761#0.458602#0.156321#0.402434#0.963374#0.300575#0.418172#0.217602#0.052088#0.188648#0.33606#0.06568#0.1747#0.508521#0.213941#0.738942");
			// pure shade csv change
			setAgeintWeightsFromString("0.467960822634352#0.93626300832447#0.0457344493086834#0.120020482935382#0.975302932170064#0.921673069646392#0.255796489193683#0.0271802681395013#0.921000898316049#0.757045808428863#0.180260463318169#0.0862796522323532#0.820517756196796#0.350585034841025#0.0951102963934901#0.409505576604415#0.998403009457943#0.889797999546068#0.0882740501991373#0.357023055402874#0.588025788191756");








		}

		public void setAgentWeights(double[] w) {
			this.weights = new Dictionary<string, double>();
			this.weights.Add(HERO_HEALTH_REDUCED, w[0]);
			this.weights.Add(HERO_ATTACK_REDUCED, w[1]);
			this.weights.Add(MINION_HEALTH_REDUCED, w[2]);
			this.weights.Add(MINION_ATTACK_REDUCED, w[3]);
			this.weights.Add(MINION_APPEARED, w[4]);
			this.weights.Add(MINION_KILLED, w[5]);
			this.weights.Add(SECRET_REMOVED, w[6]);
			this.weights.Add(MANA_REDUCED, w[7]);
			this.weights.Add(M_HEALTH, w[8]);
			this.weights.Add(M_ATTACK, w[9]);
			this.weights.Add(M_HAS_CHARGE, w[10]);
			this.weights.Add(M_HAS_DEAHTRATTLE, w[11]);
			this.weights.Add(M_HAS_DIVINE_SHIELD, w[12]);
			this.weights.Add(M_HAS_INSPIRE, w[13]);
			this.weights.Add(M_HAS_LIFE_STEAL, w[14]);
			this.weights.Add(M_HAS_STEALTH, w[15]);
			this.weights.Add(M_HAS_TAUNT, w[16]);
			this.weights.Add(M_HAS_WINDFURY, w[17]);
			this.weights.Add(M_RARITY, w[18]);
			this.weights.Add(M_MANA_COST, w[19]);
			this.weights.Add(M_POISONOUS, w[20]);

		}

		public void setAgeintWeightsFromString(string weights) {
			debug("Setting agent weights from string");
			string[] vs = weights.Split("#");

			if (vs.Length != ShadeLikeParametricGreedyAgent.NUM_PARAMETERS)
				throw new Exception("NUM VALUES NOT CORRECT");

			double[] ws = new double[ShadeLikeParametricGreedyAgent.NUM_PARAMETERS];
			for (int i = 0; i < ws.Length; i++)
			{
				ws[i] = Double.Parse(vs[i], CultureInfo.InvariantCulture);
			}

			this.setAgentWeights(ws);
		}
		public override void InitializeGame()
		{
			
		}

		private string stringTask(PlayerTask task) {
			string t = "TASK: " + task.PlayerTaskType + " " + task.Source + "----->" + task.Target;
			if (task.Target != null)
				t=t+task.Target.Controller.PlayerId;
			else
				t=t+"No target";
			return t;
		}

		private string stringMinion(Minion m) {
			return m+" "+m.AttackDamage+"/"+m.Health;
		}

		private void debug(string line) {
			if(false)
				Console.WriteLine(line);
		}
	}
}
