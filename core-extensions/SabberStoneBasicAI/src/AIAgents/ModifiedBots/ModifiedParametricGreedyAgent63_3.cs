/*
 * ParametricGreedyAgent.cs
 * 
 * Copyright (c) 2018, Pablo Garcia-Sanchez. All rights reserved.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301  USA
 * 
 * Contributors:
 * Alberto Tonda (INRA)
 */

using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;
using SabberStoneBasicAI.PartialObservation;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStoneBasicAI.AIAgents
{
	class ModifiedParametricGreedyAgent63_3 : AbstractAgent
	{
		public override void FinalizeAgent()
		{
			
		}

		public override void FinalizeGame()
		{
			
		}

		public static int NUM_PARAMETERS_PER_PHASE = 21;
		public static int NUM_PHASES = 3;
		public static int NUM_PARAMETERS = NUM_PARAMETERS_PER_PHASE * NUM_PHASES;
		public Dictionary<string, double>[] phaseWeights;
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
		private int getPhaseIndex(POGame game)
		{
			int turn = game.Turn;

			if (turn <= 3)
				return 0;
			if (turn <= 6)
				return 1;
			return 2;
		}

		private Dictionary<string, double> getWeightsForPhase(POGame game)
		{
			return phaseWeights[getPhaseIndex(game)];
		}

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
		public double scoreTask(POGame before, POGame after, Dictionary<string, double> weights) {
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
			double enemyPoints = calculateScoreHero(before.CurrentOpponent,after.CurrentOpponent, weights);
			debug("CALCULATING MY HEALTH SCORE");
			double myPoints    = calculateScoreHero(before.CurrentPlayer, after.CurrentPlayer, weights);
			debug("Enemy points: " + enemyPoints + " My points: " + myPoints);

			//Differences in Minions
			debug("CALCULATING ENEMY MINIONS");			
			double scoreEnemyMinions = calculateScoreMinions(before.CurrentOpponent.BoardZone,after.CurrentOpponent.BoardZone, weights);
			debug("Score enemy minions: " +scoreEnemyMinions);
			debug("CALCULATING MY MINIONS");
			double scoreMyMinions = calculateScoreMinions(before.CurrentPlayer.BoardZone, after.CurrentPlayer.BoardZone, weights);
			debug("Score my minions: " + scoreMyMinions);

			//Differences in Secrets
			debug("CALCULATING SECRETS");			
			double scoreEnemySecrets = calculateScoreSecretsRemoved(before.CurrentOpponent, after.CurrentOpponent, weights);
			double scoreMySecrets    = calculateScoreSecretsRemoved(before.CurrentPlayer, after.CurrentPlayer, weights);


			//Difference in Mana
			int usedMana = before.CurrentPlayer.RemainingMana - after.CurrentPlayer.RemainingMana;
			double scoreManaUsed = usedMana * weights[MANA_REDUCED];
			debug("Final task score" + enemyPoints + ",neg("+ myPoints + ")," + scoreEnemyMinions + ",neg(" + scoreMyMinions+"),S:"+ scoreEnemySecrets+"neg( " +scoreMySecrets+ ") M:neg(:"+scoreManaUsed+")");			
			return enemyPoints - myPoints + scoreEnemyMinions - scoreMyMinions+ scoreEnemySecrets - scoreMySecrets - scoreManaUsed;
		}

		double calculateScoreHero(Controller playerBefore, Controller playerAfter, Dictionary<string, double> weights) {
			debug(playerBefore.Hero.Health + "("+playerBefore.Hero.Armor+")/"+playerBefore.Hero.AttackDamage+" --> "+
				 playerAfter.Hero.Health + "(" + playerAfter.Hero.Armor + ")/" + playerAfter.Hero.AttackDamage
				);
			int diffHealth = (playerBefore.Hero.Health + playerBefore.Hero.Armor) - (playerAfter.Hero.Health+playerAfter.Hero.Armor);
			int diffAttack = (playerBefore.Hero.AttackDamage) - (playerAfter.Hero.AttackDamage);
			//debug("DIFS"+diffHealth + " " + diffAttack);
			double score = diffHealth * weights[HERO_HEALTH_REDUCED] + diffAttack * weights[HERO_ATTACK_REDUCED];
			return score;
		}

		double calculateScoreMinions(SabberStoneCore.Model.Zones.BoardZone before, SabberStoneCore.Model.Zones.BoardZone after, Dictionary<string, double> weights) {
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
						scoreHealthReduced = scoreHealthReduced + weights[MINION_HEALTH_REDUCED]*(mb.Health - ma.Health)*scoreMinion(mb, weights); //Positive points if health is reduced
						scoreAttackReduced = scoreAttackReduced + weights[MINION_ATTACK_REDUCED]*(mb.AttackDamage - ma.AttackDamage)*scoreMinion(mb, weights); //Positive points if attack is reduced
						survived = true;
						
					}
				}

				if (survived == false)
				{
					debug(stringMinion(mb) + " was killed");
					scoreKilled = scoreKilled + scoreMinion(mb, weights)*weights[MINION_KILLED]; //WHATEVER //Positive points if card is dead
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
					scoreAppeared = scoreAppeared + scoreMinion(ma, weights)*weights[MINION_APPEARED]; //Negative if a minion appeared (below)
				}
			}

			//Think always as positive points if the enemy suffers!
			return scoreHealthReduced+scoreAttackReduced+scoreKilled-scoreAppeared; //CHANGE THESE SIGNS ACCORDINGLY!!!

		}

		double calculateScoreSecretsRemoved(Controller playerBefore, Controller playerAfter, Dictionary<string, double> weights) {

			int dif = playerBefore.SecretZone.Count - playerAfter.SecretZone.Count;
			/*if (dif != 0) {
				Console.WriteLine("STOP");
			}*/
			//int dif = playerBefore.NumSecretsPlayedThisGame - playerAfter.NumSecretsPlayedThisGame;
			return dif * weights[SECRET_REMOVED];
		}

		double scoreMinion(Minion m, Dictionary<string, double> weights) {
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

			Dictionary<string, double> weights = getWeightsForPhase(state);
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
					score = scoreTask(state, nextState, weights); //Warning: if using tree, avoid overflow with max values!
					
					
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
			setAgeintWeightsFromString("0.0066572836714795#0.9636184931393563#0.9905819580183692#0.4942187608676458#0.8963702450188424#0.9136667608189339#0.2119232663034786#0.0242245774365194#0.4198005025295153#0.8175982560372805#0.0479072899333061#0.4432227124668750#0.3672707253917039#0.1383305531940957#0.9828416613197569#0.8911286011250421#0.8139617583009368#0.9836289722199139#0.5026971460733857#0.7269109319874086#0.4709372038096007#0.1793449216677268#0.0641829582199041#0.0724589185242829#0.7298376260085392#0.8434466838557535#0.9017066415850585#0.8855693054027024#0.0859874239202790#0.6939277272813006#0.3702475702071595#0.2090702174614753#0.4688813483470209#0.5016120632397337#0.9278833015692080#0.8875387270763543#0.0799104203813420#0.0211783626373962#0.2265335348083784#0.2336104676388428#0.0469960337384832#0.3614433408485515#0.7824946708010594#0.7429110173678058#0.0450078195058055#0.1075934738319906#0.6003238107757540#0.7772977125079410#0.9388892873113392#0.2001679445779777#0.9166717201744607#0.8753524471270168#0.6008056957259675#0.5673155215550927#0.0750767948332570#0.8892044878215859#0.5280315235197289#0.4277596309821602#0.0147478696087410#0.6731006256637071#0.2067001502689669#0.3813857738617193#0.0278457865346123");

		}

		public void setAgentWeights(double[] w)
		{
			if (w.Length != NUM_PARAMETERS)
				throw new Exception("NUM VALUES NOT CORRECT");

			phaseWeights = new Dictionary<string, double>[NUM_PHASES];

			for (int phase = 0; phase < NUM_PHASES; phase++)
			{
				int offset = phase * NUM_PARAMETERS_PER_PHASE;
				Dictionary<string, double> weights = new Dictionary<string, double>();

				weights.Add(HERO_HEALTH_REDUCED, w[offset + 0]);
				weights.Add(HERO_ATTACK_REDUCED, w[offset + 1]);
				weights.Add(MINION_HEALTH_REDUCED, w[offset + 2]);
				weights.Add(MINION_ATTACK_REDUCED, w[offset + 3]);
				weights.Add(MINION_APPEARED, w[offset + 4]);
				weights.Add(MINION_KILLED, w[offset + 5]);
				weights.Add(SECRET_REMOVED, w[offset + 6]);
				weights.Add(MANA_REDUCED, w[offset + 7]);
				weights.Add(M_HEALTH, w[offset + 8]);
				weights.Add(M_ATTACK, w[offset + 9]);
				weights.Add(M_HAS_CHARGE, w[offset + 10]);
				weights.Add(M_HAS_DEAHTRATTLE, w[offset + 11]);
				weights.Add(M_HAS_DIVINE_SHIELD, w[offset + 12]);
				weights.Add(M_HAS_INSPIRE, w[offset + 13]);
				weights.Add(M_HAS_LIFE_STEAL, w[offset + 14]);
				weights.Add(M_HAS_STEALTH, w[offset + 15]);
				weights.Add(M_HAS_TAUNT, w[offset + 16]);
				weights.Add(M_HAS_WINDFURY, w[offset + 17]);
				weights.Add(M_RARITY, w[offset + 18]);
				weights.Add(M_MANA_COST, w[offset + 19]);
				weights.Add(M_POISONOUS, w[offset + 20]);

				phaseWeights[phase] = weights;
			}
		}

		public void setAgeintWeightsFromString(string weights)		{
			debug("Setting agent weights from string");
			string[] vs = weights.Split("#");

			if (vs.Length != ModifiedParametricGreedyAgent63.NUM_PARAMETERS)
				throw new Exception("NUM VALUES NOT CORRECT");

			double[] ws = new double[ModifiedParametricGreedyAgent63.NUM_PARAMETERS];
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
