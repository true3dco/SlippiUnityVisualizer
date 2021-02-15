using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Slippi {
	public class SlippiLookupTable : MonoBehaviour
	{
		public static string GetCharacterName(int characterID)
		{
			var charName = "";
			switch (characterID)
			{
				case 0:
					charName = "Captain_Falcon";
					break;
				case 1:
					charName = "Dk";
					break;
				case 2:
					charName = "Fox";
					break;
				case 3:
					charName = "G&W";
					break;
				case 4:
					charName = "Kirby";
					break;
				case 5:
					charName = "Bowser";
					break;
				case 6:
					charName = "Link";
					break;
				case 7:
					charName = "Luigi";
					break;
				case 8:
					charName = "Mario";
					break;
				case 9:
					charName = "Marth";
					break;
				case 10:
					charName = "Mewtwo";
					break;
				case 11:
					charName = "Ness";
					break;
				case 12:
					charName = "Peach";
					break;
				case 13:
					charName = "Pikachu";
					break;
				case 14:
					charName = "Missing_Character"; //""Ice_Climbers";
					break;
				case 15:
					charName = "Jigglypuff";
					break;
				case 16:
					charName = "Samus";
					break;
				case 17:
					charName = "Yoshi";
					break;
				case 18:
					charName = "Zelda";
					break;
				case 19:
					charName = "Sheik";
					break;
				case 20:
					charName = "Falco";
					break;
				case 21:
					charName = "Young_Link";
					break;
				case 22:
					charName = "Doctor_Mario";
					break;
				case 23:
					charName = "Roy";
					break;
				case 24:
					charName = "Pichu";
					break;
				case 25:
					charName = "Ganondorf";
					break;
			};
			return charName;
		}

		public static string GetCharacterSpecificMapping(string characterName, string animationName)
        {
			switch (characterName){
				case "Luigi":
					switch (animationName) {
						case "Wait4":
							return "SpecialSStart";				
					}
					break;

			}
		
			return null;

		}
		public static string GetStageName(int stageID)
        {

			Debug.Log("Stage ID:" + stageID);
			var stageName = "";
			switch (stageID)
			{
				case 2:
					stageName = "Fountain_Of_Dreams";
					break;
				case 3:
					stageName = "Stadium";//"Pokemon_Stadium";
					break;
				case 8:
					stageName = "Yoshis_Story";//"Yoshis_Story";
					break;
				case 28:
					stageName = "Dreamland_Final";
					break;
				case 31:
					stageName = "DietBattlefield";
					break;
				case 32:
					stageName = "Final_Destination";
					break;
				default:
					Debug.LogWarning("MISSING STAGE");
					stageName = "Final_Destination";
					break;
			};
			return stageName;
		}


		public static bool IsAnimationSupported(string animationName){
			return true;
		}

		public static string GetAnimationName(int animID) { 

		var animName = "";
		switch (animID){
			case 0:
				animName = "DeadDown";
				break;
			case 1:
				animName = "DeadLeft";
				break;
			case 2:
				animName = "DeadRight";
				break;
			case 3:
				animName = "DeadUp";
				break;	
			case 4:
				animName = "DeadUpStar";
				break;
			case 5:
				animName = "DeadUpStarIce";
				break;
			case 6:
				animName = "";
				break;
			case 7:
				animName = "DeadUpFallHitCamera";
				break;
			case 8:
				animName = "DeadUpFallHitCameraFlat";
				break;
			case 9:
				animName = "DeadUpFallIce";
				break;
			case 10:
				animName = "DeadUpFallHitCameraIce";
				break;
			case 11:
				animName = "Sleep";
				break;
			case 12:
				animName = "Rebirth";
				break;
			case 13:
				animName = "RebirthWait";
				break;
			case 14:
				animName = "Wait1";
				break;
			case 15:
				animName = "WalkSlow";
				break;
			case 16:
				animName = "WalkMiddle";
				break;
			case 17:
				animName = "WalkFast";
				break;
			case 18:
				animName = "Turn";
				break;
			case 19:
				animName = "TurnRun";
				break;
			case 20:
				animName = "Dash";
				break;
			case 21:
				animName = "Run";
				break;
			case 22:
				animName = "RunDirect";
				break;
			case 23:
				animName = "RunBrake";
				break;
			case 24:
				animName = "Landing";
				break;
			case 25:
				animName = "JumpF";
				break;
			case 26:
				animName = "JumpB";
				break;
			case 27:
				animName = "JumpAerialF";
				break;
			case 28:
				animName = "JumpAerialB";
				break;
			case 29:
				animName = "Fall";
				break;
			case 30:
				animName = "FallF";
				break;
			case 31:
				animName = "FallB";
				break;
			case 32:
				animName = "FallAerial";
				break;
			case 33:
				animName = "FallAerialF";
				break;
			case 34:
				animName = "FallAerialB";
				break;
			case 35:
				animName = "FallSpecial";
				break;
			case 36:
				animName = "FallSpecialF";
				break;
			case 37:
				animName = "FallSpecialB";
				break;
			case 38:
				animName = "DamageFall";
				break;
			case 39:
				animName = "Squat";
				break;
			case 40:
				animName = "SquatWait";
				break;
			case 41:
				animName = "SquatRv";
				break;
			case 42:
				animName = "Landing";
				break;
			case 43:
				animName = "LandingFallSpecial";
				break;
			case 44:
				animName = "Attack11";
				break;
			case 45:
				animName = "Attack12";
				break;
			case 46:
				animName = "Attack13";
				break;
			case 47:
				animName = "Attack100Start";
				break;
			case 48:
				animName = "Attack100Loop";
				break;
			case 49:
				animName = "Attack100End";
				break;
			case 50:
				animName = "AttackDash";
				break;
			case 51:
				animName = "AttackS3Hi";
				break;
			case 52:
				animName = "AttackS3HiS";
				break;
			case 53:
				animName = "AttackS3S";
				break;
			case 54:
				animName = "AttackS3LwS";
				break;
			case 55:
				animName = "AttackS3Lw";
				break;
			case 56:
				animName = "AttackHi3";
				break;
			case 57:
				animName = "AttackLw3";
				break;
			case 58:
				animName = "AttackS4Hi";
				break;
			case 59:
				animName = "AttackS4HiS";
				break;
			case 60:
				animName = "AttackS4S";
				break;
			case 61:
				animName = "AttackS4LwS";
				break;
			case 62:
				animName = "AttackS4Lw";
				break;
			case 63:
				animName = "AttackHi4";
				break;
			case 64:
				animName = "AttackLw4";
				break;
			case 65:
				animName = "AttackAirN";
				break;
			case 66:
				animName = "AttackAirF";
				break;
			case 67:
				animName = "AttackAirB";
				break;
			case 68:
				animName = "AttackAirHi";
				break;
			case 69:
				animName = "AttackAirLw";
				break;
			case 70:
				animName = "LandingAirN";
				break;
			case 71:
				animName = "LandingAirF";
				break;
			case 72:
				animName = "LandingAirB";
				break;
			case 73:
				animName = "LandingAirHi";
				break;
			case 74:
				animName = "LandingAirLw";
				break;
			case 75:
				animName = "DamageHi1";
				break;
			case 76:
				animName = "DamageHi2";
				break;
			case 77:
				animName = "DamageHi3";
				break;
			case 78:
				animName = "DamageN1";
				break;
			case 79:
				animName = "DamageN2";
				break;
			case 80:
				animName = "DamageN3";
				break;
			case 81:
				animName = "DamageLw1";
				break;
			case 82:
				animName = "DamageLw2";
				break;
			case 83:
				animName = "DamageLw3";
				break;
			case 84:
				animName = "DamageAir1";
				break;
			case 85:
				animName = "DamageAir2";
				break;
			case 86:
				animName = "DamageAir3";
				break;
			case 87:
				animName = "DamageFlyHi";
				break;
			case 88:
				animName = "DamageFlyN";
				break;
			case 89:
				animName = "DamageFlyLw";
				break;
			case 90:
				animName = "DamageFlyTop";
				break;
			case 91:
				animName = "DamageFlyRoll";
				break;
			case 92:
				animName = "LightGet";
				break;
			case 93:
				animName = "HeavyGet";
				break;
			case 94:
				animName = "LightThrowF";
				break;
			case 95:
				animName = "LightThrowB";
				break;
			case 96:
				animName = "LightThrowHi";
				break;
			case 97:
				animName = "LightThrowLw";
				break;
			case 98:
				animName = "LightThrowDash";
				break;
			case 99:
				animName = "LightThrowDrop";
				break;
			case 100:
				animName = "LightThrowAirF";
				break;
			case 101:
				animName = "LightThrowAirB";
				break;
			case 102:
				animName = "LightThrowAirHi";
				break;
			case 103:
				animName = "LightThrowAirLw";
				break;
			case 104:
				animName = "HeavyThrowF";
				break;
			case 105:
				animName = "HeavyThrowB";
				break;
			case 106:
				animName = "HeavyThrowHi";
				break;
			case 107:
				animName = "HeavyThrowLw";
				break;
			case 108:
				animName = "LightThrowF4";
				break;
			case 109:
				animName = "LightThrowB4";
				break;
			case 110:
				animName = "LightThrowHi4";
				break;
			case 111:
				animName = "LightThrowLw4";
				break;
			case 112:
				animName = "LightThrowAirF4";
				break;
			case 113:
				animName = "LightThrowAirB4";
				break;
			case 114:
				animName = "LightThrowAirHi4";
				break;
			case 115:
				animName = "LightThrowAirLw4";
				break;
			case 116:
				animName = "HeavyThrowF4"; //Here
				break;
			case 117:
				animName = "HeavyThrowB4";
				break;
			case 118:
				animName = "HeavyThrowHi4";
				break;
			case 119:
				animName = "HeavyThrowLw4";
				break;
			case 120:
				animName = "SwordSwing1";
				break;
			case 121:
				animName = "SwordSwing3";
				break;
			case 122:
				animName = "SwordSwing4";
				break;
			case 123:
				animName = "SwordSwingDash";
				break;
			case 124:
				animName = "BatSwing1";
				break;
			case 125:
				animName = "BatSwing3";
				break;
			case 126:
				animName = "BatSwing4";
				break;
			case 127:
				animName = "BatSwingDash";
				break;
			case 128:
				animName = "ParasolSwing1";
				break;
			case 129:
				animName = "ParasolSwing3";
				break;
			case 130:
				animName = "ParasolSwing4";
				break;
			case 131:
				animName = "ParasolSwingDash";
				break;
			case 132:
				animName = "HarisenSwing1";
				break;
			case 133:
				animName = "HarisenSwing3";
				break;
			case 134:
				animName = "HarisenSwing4";
				break;
			case 135:
				animName = "HarisenSwingDash";
				break;
			case 136:
				animName = "StarRodSwing1";
				break;
			case 137:
				animName = "StarRodSwing3";
				break;
			case 138:
				animName = "StarRodSwing4";
				break;
			case 139:
				animName = "StarRodSwingDash";
				break;
			case 140:
				animName = "LipStickSwing1";
				break;
			case 141:
				animName = "LipStickSwing3";
				break;
			case 142:
				animName = "LipStickSwing4";
				break;
			case 143:
				animName = "LipStickSwingDash";
				break;
			case 144:
				animName = "ItemParasolOpen";
				break;
			case 145:
				animName = "ItemParasolFall";
				break;
			case 146:
				animName = "ItemParasolFallSpecial";
				break;
			case 147:
				animName = "ItemParasolDamageFall";
				break;
			case 148:
				animName = "LGunShoot";
				break;
			case 149:
				animName = "LGunShootAir";
				break;
			case 150:
				animName = "LGunShootEmpty";
				break;
			case 151:
				animName = "LGunShootAirEmpty";
				break;
			case 152:
				animName = "FireFlowerShoot";
				break;
			case 153:
				animName = "FireFlowerShootAir";
				break;
			case 154:
				animName = "ItemScrew";
				break;
			case 155:
				animName = "ItemScrewAir";
				break;
			case 156:
				animName = "DamageScrew";
				break;
			case 157:
				animName = "DamageScrewAir";
				break;
			case 158:
				animName = "ItemScopeStart";
				break;
			case 159:
				animName = "ItemScopeRapid";
				break;
			case 160:
				animName = "ItemScopeFire";
				break;
			case 161:
				animName = "ItemScopeEnd";
				break;
			case 162:
				animName = "ItemScopeAirStart";
				break;
			case 163:
				animName = "ItemScopeAirRapid";
				break;
			case 164:
				animName = "ItemScopeAirFire";
				break;
			case 165:
				animName = "ItemScopeAirEnd";
				break;
			case 166:
				animName = "ItemScopeStartEmpty";
				break;
			case 167:
				animName = "ItemScopeRapidEmpty";
				break;
			case 168:
				animName = "ItemScopeFireEmpty";
				break;
			case 169:
				animName = "ItemScopeEndEmpty";
				break;
			case 170:
				animName = "ItemScopeAirStartEmpty"; //Here
				break;
			case 171:
				animName = "ItemScopeAirRapidEmpty";
				break;
			case 172:
				animName = "ItemScopeAirFireEmpty";
				break;
			case 173:
				animName = "ItemScopeAirEndEmpty";
				break;
			case 174:
				animName = "LiftWait";
				break;
			case 175:
				animName = "LiftWalk1";
				break;
			case 176:
				animName = "LiftWalk2";
				break;
			case 177:
				animName = "LiftTurn";
				break;
			case 178:
				animName = "GuardOn";
				break;
			case 179:
				animName = "Guard";
				break;
			case 180:
				animName = "GuardOff";
				break;
			case 181:
				animName = "GuardSetOff";
				break;
			case 182:
				animName = "GuardReflect";
				break;
			case 183:
				animName = "DownBoundU";
				break;
			case 184:
				animName = "DownWaitU";
				break;
			case 185:
				animName = "DownDamageU";
				break;
			case 186:
				animName = "DownStandU";
				break;
			case 187:
				animName = "DownAttackU";
				break;
			case 188:
				animName = "DownFowardU";
				break;
			case 189:
				animName = "DownBackU";
				break;
			case 190:
				animName = "DownSpotU";
				break;
			case 191:
				animName = "DownBoundD";
				break;
			case 192:
				animName = "DownWaitD";
				break;
			case 193:
				animName = "DownDamageD";
				break;
			case 194:
				animName = "DownStandD";
				break;
			case 195:
				animName = "DownAttackD";
				break;
			case 196:
				animName = "DownFowardD";
				break;
			case 197:
				animName = "DownBackD";
				break;
			case 198:
				animName = "DownSpotD";
				break;
			case 199:
				animName = "Passive";
				break;
			case 200:
				animName = "PassiveStandF";
				break;
			case 201:
				animName = "PassiveStandB";
				break;
			case 202:
				animName = "PassiveWall";
				break;
			case 203:
				animName = "PassiveWallJump";
				break;
			case 204:
				animName = "PassiveCeil";
				break;
			case 205:
				animName = "ShieldBreakFly";
				break;
			case 206:
				animName = "ShieldBreakFall";
				break;
			case 207:
				animName = "ShieldBreakDownU";
				break;
			case 208:
				animName = "ShieldBreakDownD";
				break;
			case 209:
				animName = "ShieldBreakStandU";
				break;
			case 210:
				animName = "ShieldBreakStandD";
				break;
			case 211:
				animName = "FuraFura";
				break;
			case 212:
				animName = "Catch";
				break;
			case 213:
				animName = "CatchPull";
				break;
			case 214:
				animName = "CatchDash";
				break;
			case 215:
				animName = "CatchDashPull";
				break;
			case 216:
				animName = "CatchWait";
				break;
			case 217:
				animName = "CatchAttack";
				break;
			case 218:
				animName = "CatchCut";
				break;
			case 219:
				animName = "ThrowF";
				break;
			case 220:
				animName = "ThrowB";
				break;
			case 221:
				animName = "ThrowHi";
				break;
			case 222:
				animName = "ThrowLw";
				break;
			case 223:
				animName = "CapturePulledHi";
				break;
			case 224:
				animName = "CaptureWaitHi";
				break;
			case 225:
				animName = "CaptureDamageHi";
				break;
			case 226:
				animName = "CapturePulledLw";
				break;
			case 227:
				animName = "CaptureWaitLw";
				break;
			case 228:
				animName = "CaptureDamageLw";
				break;
			case 229:
				animName = "CaptureCut";
				break;
			case 230:
				animName = "CaptureJump";
				break;
			case 231:
				animName = "CaptureNeck";
				break;
			case 232:
				animName = "CaptureFoot";
				break;
			case 233:
				animName = "EscapeF";
				break;
			case 234:
				animName = "EscapeB";
				break;
			case 235:
				animName = "Escape";
				break;
			case 236:
				animName = "EscapeAir";
				break;
			case 237:
				animName = "ReboundStop";
				break;
			case 238:
				animName = "Rebound";
				break;
			case 239:
				animName = "ThrownF";
				break;
			case 240:
				animName = "ThrownB";
				break;
			case 241:
				animName = "ThrownHi";
				break;
			case 242:
				animName = "ThrownLw";
				break;
			case 243:
				animName = "ThrownLwWomen";
				break;
			case 244:
				animName = "Pass";
				break;
			case 245:
				animName = "Ottotto";
				break;
			case 246:
				animName = "OttottoWait";
				break;
			case 247:
				animName = "FlyReflectWall";
				break;
			case 248:
				animName = "FlyReflectCeil";
				break;
			case 249:
				animName = "StopWall";
				break;
			case 250:
				animName = "StopCeil";
				break;
			case 251:
				animName = "MissFoot";
				break;
			case 252:
				animName = "CliffCatch";
				break;
			case 253:
				animName = "CliffWait";
				break;
			case 254:
				animName = "CliffClimbSlow";
				break;
			case 255:
				animName = "CliffClimbQuick";
				break;
			case 256:
				animName = "CliffAttackSlow";
				break;
			case 257:
				animName = "CliffAttackQuick";
				break;
			case 258:
				animName = "CliffEscapeSlow";
				break;
			case 259:
				animName = "CliffEscapeQuick";
				break;
			case 260:
				animName = "CliffJumpSlow1";
				break;
			case 261:
				animName = "CliffJumpSlow2";
				break;
			case 262:
				animName = "CliffJumpQuick1";
				break;
			case 263:
				animName = "CliffJumpQuick2";
				break;
			case 264:
				animName = "AppealR";
				break;
			case 265:
				animName = "AppealL";
				break;
			case 266:
				animName = "ShoulderedWait";
				break;
			case 267:
				animName = "ShoulderedWalkSlow";
				break;
			case 268:
				animName = "ShoulderedWalkMiddle";
				break;
			case 269:
				animName = "ShoulderedWalkFast";
				break;
			case 270:
				animName = "ShoulderedTurn";
				break;
			case 271:
				animName = "ThrownFF";
				break;
			case 272:
				animName = "ThrownFB";
				break;
			case 273:
				animName = "ThrownFHi";
				break;
			case 274:
				animName = "ThrownFLw";
				break;
			case 275:
				animName = "CaptureCaptain";
				break;
			case 276:
				animName = "CaptureYoshi";
				break;
			case 277:
				animName = "YoshiEgg";
				break;
			case 278:
				animName = "CaptureKoopa";
				break;
			case 279:
				animName = "CaptureDamageKoopa";
				break;
			case 280:
				animName = "CaptureWaitKoopa";
				break;
			case 281:
				animName = "ThrownKoopaF";
				break;
			case 282:
				animName = "ThrownKoopaB";
				break;
			case 283:
				animName = "CaptureKoopaAir";
				break;
			case 284:
				animName = "CaptureDamageKoopaAir";
				break;
			case 285:
				animName = "CaptureWaitKoopaAir";
				break;
			case 286:
				animName = "ThrownKoopaAirF";
				break;
			case 287:
				animName = "ThrownKoopaAirB";
				break;
			case 288:
				animName = "CaptureKirby";
				break;
			case 289:
				animName = "CaptureWaitKirby";
				break;
			case 290:
				animName = "ThrownKirbyStar";
				break;
			case 291:
				animName = "ThrownCopyStar";
				break;
			case 292:
				animName = "ThrownKirby";
				break;
			case 293:
				animName = "BarrelWait";
				break;
			case 294:
				animName = "Bury";
				break;
			case 295:
				animName = "BuryWait";
				break;
			case 296:
				animName = "BuryJump";
				break;
			case 297:
				animName = "DamageSong";
				break;
			case 298:
				animName = "DamageSongWait";
				break;
			case 299:
				animName = "DamageSongRv";
				break;
			case 300:
				animName = "DamageBind";
				break;
			case 301:
				animName = "CaptureMewtwo";
				break;
			case 302:
				animName = "CaptureMewtwoAir";
				break;
			case 303:
				animName = "ThrownMewtwo";
				break;
			case 304:
				animName = "ThrownMewtwoAir";
				break;
			case 305:
				animName = "WarpStarJump";
				break;
			case 306:
				animName = "WarpStarFall";
				break;
			case 307:
				animName = "HammerWait";
				break;
			case 308:
				animName = "HammerWalk";
				break;
			case 309:
				animName = "HammerTurn";
				break;
			case 310:
				animName = "HammerKneeBend";
				break;
			case 311:
				animName = "HammerFall";
				break;
			case 312:
				animName = "HammerJump";
				break;
			case 313:
				animName = "HammerLanding";
				break;
			case 314:
				animName = "KinokoGiantStart";
				break;
			case 315:
				animName = "KinokoGiantStartAir";
				break;
			case 316:
				animName = "KinokoGiantEnd";
				break;
			case 317:
				animName = "KinokoGiantEndAir";
				break;
			case 318:
				animName = "KinokoSmallStart";
				break;
			case 319:
				animName = "KinokoSmallStartAir";
				break;
			case 320:
				animName = "KinokoSmallEnd";
				break;
			case 321:
				animName = "KinokoSmallEndAir";
				break;
			case 322:
				animName = "Entry";
				break;
			case 323:
				animName = "Entry";
				//really: animName = "EntryStart";
				break;
			case 324:
				animName = "Entry";
				// really: animName = "EntryEnd";
				break;
			case 325:
				animName = "DamageIce";
				break;
			case 326:
				animName = "DamageIceJump";
				break;
			case 327:
				animName = "CaptureMasterhand";
				break;
			case 328:
				animName = "CapturedamageMasterhand";
				break;
			case 329:
				animName = "CapturewaitMasterhand";
				break;
			case 330:
				animName = "ThrownMasterhand";
				break;
			case 331:
				animName = "CaptureKirbyYoshi";
				break;
			case 332:
				animName = "KirbyYoshiEgg";
				break;
			case 333:
				animName = "CaptureLeadead";
				break;
			case 334:
				animName = "CaptureLikelike";
				break;
			case 335:
				animName = "DownReflect";
				break;
			case 336:
				animName = "CaptureCrazyhand";
				break;
			case 337:
				animName = "CapturedamageCrazyhand";
				break;
			case 338:
				animName = "CapturewaitCrazyhand";
				break;
			case 339:
				animName = "ThrownCrazyhand";
				break;
			case 340:
				animName = "BarrelCannonWait";
				break;
			case 341:
				animName = "Wait1";
				break;
			case 342:
				animName = "Wait2";
				break;
			case 343:
				animName = "Wait3";
				break;
			case 344:
				animName = "Wait4";
				break;
			case 345:
				animName = "SpecialAirNStart";
				break;
			case 346:
				animName = "SpecialAirNLoop";
				break;
			case 347:
				animName = "SpecialAirNEnd";
				break;
			case 348:
				animName = "SpecialAirNEnd";
				break;
			case 349:
				animName = "SpecialS1";
				break;
			case 350:
				animName = "SpecialS2Hi";
				break;
			case 351:
				animName = "SpecialS2Lw";
				break;
			case 352:
				animName = "SpecialS3Hi";
				break;
			case 353:
				animName = "SpecialS3S";
				break;
			case 354:
				animName = "SpecialS3Lw";
				break;
			case 355:
				animName = "ItemHammerMove";
				break;
			case 356:
				animName = "ItemBlind";
				break;
			case 357:
				animName = "DamageElec";
				break;
			case 358:
				animName = "SpecialAirS1";
				break;
			case 359:
				animName = "FuraSleepLoop";
				break;
			case 360:
				animName = "FuraSleepEnd";
				break;
			case 361:
				animName = "WallDamage";
				break;
			case 362:
				animName = "CliffWait1";
				break;
			case 363:
				animName = "CliffWait2";
				break;
			case 364:
				animName = "SlipDown";
				break;
			case 365:
				animName = "Slip";
				break;
			case 366:
				animName = "SlipTurn";
				break;
			case 367:
				animName = "SpecialHi";
				break;
			case 368:
				animName = "SpecialAirHi";
				break;
			case 369:
				animName = "SlipStand";
				break;
			case 370:
				animName = "SlipAttack";
				break;
			case 371:
				animName = "SlipEscapeF";
				break;
			case 372:
				animName = "SlipEscapeB";
				break;
			case 373:
				animName = "AppealS";
				break;
			case 374:
				animName = "Zitabata";
				break;
			case 375:
				animName = "CaptureKoopaHit";
				break;
			case 376:
				animName = "ThrownKoopaEndF";
				break;
			case 377:
				animName = "ThrownKoopaEndB";
				break;
			case 378:
				animName = "CaptureKoopaAirHit";
				break;
			case 379:
				animName = "ThrownKoopaAirEndF";
				break;
			case 380:
				animName = "ThrownKoopaAirEndB";
				break;
			case 381:
				animName = "ThrownKirbyDrinkSShot";
				break;
			case 382:
				animName = "ThrownKirbySpitSShot"; //Done
				break;
			default:
				Debug.LogWarning("Unfound Animation ID: " + animID);
				break;
		};
		return animName;
		}
}
}