# Project Requirements

## Executive Summary

This project aims to develop an educational video game that helps children understand how air quality affects health in an engaging and interactive way. The game addresses the challenge of making concepts such as air pollution, air circulation, and the Air Quality Health Index easy to understand and meaningful for young kids. Players travel across the cities in Canada, to avoid pollution sources such as wildfires and factories, and use clean-air strategies to keep their Lung Meter healthy. The product’s core functionality combines learning with decision-making, where players earn points by choosing effective environmental strategies that also change the game world and their path through it. The game will be used by children as a learning tool, either independently or in educational settings, to explore air quality concepts while actively applying their knowledge through gameplay.

## Project Glossary

### Core Game

- **Pollution Level** - A variable to track the session throughout. It is visualized as a "Lung Cup" filling up asthe risk increases.
- **Lung Cup** - A container to represent the player's health. It starts at zero and fills up as the pollution level increases.
- **Pompoms** - Items that get added to the Lung Cup to count the pollution level, where 0 represents clean lungs and 10 represents a "Game Over".
- **Buff** - A specific ability to a specific character of the game when the player hovers over the avatar during the selection.
- **Juice Mode** - A reward when three questions are answered correctly in a row, granting the character a rainbow trail and immunity to the next Grey Spot.
- **Win Rate** - A way to keep track of correct answers. It is used to determine if the game should switch the question difficulty to Beginner or Expert.
- **Risk Levels** - The levels of air quality danger displayed on the map. The levels are Low Risk (1-3), Moderate Risk (4-6), High Risk (7-10), and Very High Risk (10+).
- **Air Hero** - The highest scoring run achieved at the end of the game if the player finished with 0-2 pompoms in their Lung Cup.
- **Air Helper** - The middle scoring run achieved at the end of the game if the player finished with 3-6 pompoms in their Lung Cup.
- **Clean Air Solution** - Items that players must find and use while navigating the map to keep their Lung meter clean.
- **Smog Sprites** - Objects that fill the screen during the "Wind Fan Mini-Game" which the player must rapidly swipe to clear away.

### Educational Terms

- **AQHI** - It stands for Air Quality Health Index, measures the health effects of short term air pollution exposure.
- **Air Pressure** - It is described as the weight of the air.
- **Scrubber** - A filter that the player must have in a factor's chimney to change black smoke into clear steam.
- **Temperature Inversion** - A random weather event where warm air traps cold and dirty air near the group. This will cause all the pollution penalties to be doubled for three turns.

### Characters and Abilities

- **Player** - Someone who is playing the game.
- **The Cyclist** - A character whose theme is "Bike Power! Zero tailpipe smoke".
- **Traffic Weaver** - The Cyclist's ability to take zero pollution damage when landing on "Car/Traffic" Grey Spots.
- **The Ranger** - A character whose theme is "Trees are the lungs of the Earth".
- **Photosynthesis Boost** - The Ranger's ability to remove double the pollution when landing on a Green Spot.
- **The Scientist** - A character whose theme is "I can see the invisible!".
- **Forecasting** - The Scientist's ability to click a "Sensor Button" to reveal the correct answer to a Quiz Card (usable twice per game).

### Map Elements

- **Blue Card** - A card that is drawn by the player, one side is solid blue, and the other has either a fact on it or a question for the player to answer.
- **Blue Spots** - It is known as the "Blue Sky Breaks" that opens a window for the player to draw a card for either a fact or answer a question.
- **Green Spots** - It is known as the "Hero Solution Zones" that shows positive actions an opens up mini-games to remove pollution from the player's Lung cup.
- **Grey Spots** - It is known as the "Air Pollution Alerts" thats located near pollution sources. This adds a point to the Pollution Level.
- **Numbered Spaces** - The sequence of path labelled 1 through 38 that the players move along to reach the finish from the start.
- **Hidden Shortcuts** - These are secret paths through the forests that be revealed by the Ranger avatar to bypass sections of the map.

## [Requirements Traceability Matrix - Google Sheets](https://docs.google.com/spreadsheets/d/1o1cWHcWgR-8uD1HNVFy-zp-Idas_VhFcwza_brM02vE/edit?gid=2117506884#gid=2117506884)

## [UI Design Principles/Heuristics + Accessibility - Google Sheets](https://docs.google.com/spreadsheets/d/1o1cWHcWgR-8uD1HNVFy-zp-Idas_VhFcwza_brM02vE/edit?gid=124615829#gid=124615829)

## User Stories

### General Board Movement

**GBM.01 - Roll Dice Movement**  
As a player, I want to be able to roll a dice on my turn and move the indicated number of spaces, in order to move toward the end.

> - On the player’s turn, a roll dice control is available
> - The dice result is a random integer from 1 to 6
> - After the roll, the player’s token moves forward exactly that many spaces along the path
> - The player does not move backward when rolling; movement is only toward the end.
> - If the player is near the end (e.g. on 36), a roll of 3 moves them to 38
> - The player cannot roll again until their turn is complete


**GBM.02 - Draw Blue Card**  
As a player, I want to draw a random blue card when I land on a blue circle, so that I can learn something about air pollution.

> - When the player lands on a blue square, a blue card is drawn (fact or question).
> - Only one card is drawn per landing on a blue square.
> - The card is displayed to the player.
> - The player cannot move or roll again until the card has been acknowledged.
> - Landing on a green or grey square does not trigger a blue card draw.

**GBM.03 - Air-Pollution Facts**  
As a player, I want some blue cards to present air-pollution facts, so that I can learn new information.

> - When the player lands on a blue square, the drawn card can be a fact card.
> - A fact card shows the text written on the card to the player.
> - Reading a fact card does not change the player’s pollution score.
> - After the player dismisses the fact, the card UI closes and normal gameplay resumes.
> - The system can randomly choose between a fact card and a quiz card when drawing from a blue square.

**GBM.04 - Blue Trivia Questions**  
As a player, I want some blue cards to be multiple-choice questions, so that I can test my understanding and be rewarded.

> - When the player lands on a blue square, the drawn card can be a quiz card with multiple-choice options.
> - The player can select exactly one answer from the options shown.
> - Each quiz card has one correct answer; the game can determine right vs wrong
> - Correct answer: The player is rewarded, that is, the pollution score decreases by 1
> - Incorrect answer: The player receives a consequence, that is the player moves backwards 1 square
> - After the player selects an answer, the result is shown and the card is dismissed before gameplay continues.

**GBM.05 - Grey Square Penalty**  
As a player, I want to gain a pollution point when I land on a grey square, so that pollution sources impact my score.

> - When the player lands on a grey square, their pollution score increases by 1.
> - The increase is applied once per landing.
> - The pollution score does not exceed the maximum.
> - The updated score is reflected in the UI.
> - Appropriate feedback is shown.
> - Landing on green or blue squares does not increase pollution (unless a wrong answer on a blue card does so).

**GBM.06 - Green Square Reward**  
As a player, I want to lose a pollution point when I land on a green square, so that clean areas reward me.

> - When the player lands on a green square, their pollution score decreases by 1.
> - The decrease is applied once per landing.
> - The pollution score does not go below 0 after the decrease.
> - The updated score is reflected in the UI.
> - Appropriate feedback is shown.
> - Landing on grey or blue squares does not by itself decrease pollution (only correct quiz answers or green squares do).

**GBM.07 - Visual Pollution Score**  
As a player, I want to see my pollution score during the game represented visually (Lungs / AQHI bar), so that I can easily understand my air quality status.

> - The player’s pollution score is always visible during gameplay.
> - The score is shown using Lungs and/or an AQHI-style bar.
> - As the score increases (e.g. landing on grey, wrong answer), the visual updates (e.g. lungs fill, bar rises) within a short time.
> - As the score decreases (e.g. landing on green, correct answer), the visual updates (e.g. lungs clear, bar falls) within a short time.
> - The score never appears out of the valid range.
> - The representation is readable and understandable.

**GBM.08 - Turn Camera Follow**  
As a player, I want the camera to follow and zoom in on my character when its my turn, so that I can focus on my actions.

> - At the start of the player’s turn, the camera centers on that player’s character.
> - When the player rolls and moves, the camera follows the character along the path.
> - The camera framing keeps the current path and a few upcoming spaces visible.
> - When the character stops on their final space for the turn, the camera stops moving and holds that view until the turn ends.
> - In multiplayer, when it becomes another player’s turn, the camera switches to follow that player’s character (shows the active player).
> - The camera does not obscure important UI.

**GBM.09 - Choose Trivia Answer**  
As a player, I want to choose an answer from a pool of multiple choice answers when landing on a blue space with a trivia question, so that I can test my understanding.

> - When the player lands on a blue square and the drawn card is a trivia/question card (not a fact), the card shows multiple-choice answers.
> - The player can click or tap an answer option to select it.
> - The player can select only one answer per question; selecting another option either replaces the first choice.
> - The selected option is visually indicated (e.g. highlight, border).
> - After the player selects an answer, the game evaluates it (correct or incorrect) and applies the outcome.
> - If the card is a fact (not a trivia question), multiple-choice options are not shown.

**GBM.10 - Final Score Display**  
As a player, I want to see my score when I reach the end of the board, so that I can know what my final air quality health is.

> - A player can start the game by choosing a character.
> - The player rolls a dice and moves forward.
> - On a grey spot, the player grabs a pom pom (representing air pollution).
> - On a blue spot, the player picks a card which can either have a fact or a question.
> - Upon getting a question, if the player answers it correctly, then the player can remove one of the air pollution from the cup.
> - If the player answers it incorrectly, the player has to move back by one spot.
> - The game continues this way.
> - When the game ends, the number of air pollution pom poms that the player finally has, determine the points of the player.
> - The game will check the corresponding AQHI with the number of air pollution pom poms collected.

**GBM.11 - Wrong Answer Backstep**  
As a player, I want to move one step back if I answer a blue card incorrectly, so that mistakes have consequences.

> - When the player answers a blue card (trivia) incorrectly, their token moves backward exactly one space on the path.
> - The backward move is applied after the wrong-answer feedback.
> - If the player is on position 1 (Start), they do not move backward.
> - The pollution score still increases by 1 for the wrong answer.
> - There is clear visual or audio feedback that the answer was wrong before or during the backward move.
> - The player’s new position is correct and visible on the board after the move.

**GBM.13 - Learn from Mistakes**  
As a player, I want to see a specific explanation message after answering a question (whether right or wrong), so that I understand why my answer is right/wrong.

> - After the player selects an answer on a blue card (trivia), an explanation message is shown.
> - The message appears immediately after the answer is selected.
> - For a correct answer, the message explains why it is right .
> - For an incorrect answer, the message explains why it is wrong and what the correct idea is.
> - The message stays visible until the player dismisses it.

### Additional Board Events

**ABE.01 - Minigame on Green Square**  
As a player, I want to play a mini-game when I land on a green square, so that my pollution reduction depends on my performance.

> - When a player lands on a green square, a mini game is triggered.
> - Landing on any non-green square does not trigger the mini game.
> - The player cannot continue their turn until the mini game is completed or exited.
> - Completing the mini game will return a result that determines how much the pollution score is reduced.
> - The pollution score is reduced by the exact amount returned.
> - Pollution score cannot go below zero after reduction.
> - The updated pollution score is displayed after the mini game ends as well.
> - After the mini game finishes, normal gameplay resumes (end turn/ next player).
> - If the mini game fails to load or is exited by user, the score is not changed and game resumes.

**ABE.02 - Start Boss Battles**  
As a player, I want to start boss battles near major sources of pollutions, so that i face harder challenges in dangerous areas.

> - Major pollution sources squares are clearly marked on the board.
> - When a player enters a major pollution area, a boss-battle is triggered.
> - The boss battle presents multiple difficult questions.
> - The player must answer all questions to complete the battle.
> - The game should track correct and incorrect answers during the battle.
> - The boss battle cannot be skipped once started.
> - The player cannot end turn or move normally until battle has ended.
> - The results of the battle will affect the pollution score or gameplay (?).
> - After battle ends, player returns to main game and normal game resumes.

**ABE.03 - Random Weather Effects**  
As a player, I want random weather effects based on the current AQHI, so that air quality feels dynamic.

> - Game tracks current AQHI level during gameplay.
> - At certain predefined points (?), the game checks whether a random weather event should occur.
> - The chance of an event occurring is influenced by the current AQHI level.
> - Higher AQHI levels increase the likelihood of negative weather effects, lower aqhi levels increase the likelihood of positive weather effects.
> - When triggered, the effect should be announced to ALL players.

**ABE.04 - Trigger Airflow Combo**  
As a player, I want to trigger a “Fresh Air Flow” combo after three correct answers in a row, so that good performance is rewarded by a rainbow trail and I can be immune to one grey square.

> - Game should track consecutive correct answers given by the player.
> - Correct answers must be given in a row without any incorrect answers.
> - When 3 questions are answered correctly in a row, the 'fresh air flow' combo is triggered.
> - When the combo is activated the players character displayes a visible rainbow trail effect.
> - When combo is activated the player is immune to the effects of the next grey square they land on.
> - After one grey square is blocked the combo is consumed and removed and the rainbow trial disappears.
> - Streak counter resets if one answer is incorrectly answered.

**ABE.05 - Fight a Boss**  
As a player, I want to fight a boss by being asked three difficult questions, so that I can test my knowledge.

> - When a player enters a major pollution / boss area on the board, a boss battle starts.
> - The boss battle UI appears and blocks normal movement until the battle ends.
> - The player is shown three questions during the battle. (one after the other).
> - The questions use the “difficult” question set, not the easy set.
> - The player cannot skip the battle or leave it once it has started.
> - The player cannot roll the dice or move on the board until all three questions have been answered.
> - After the third answer is submitted, the battle ends and the result is shown.

**ABE.06 - Beat a Boss**  
As a player, I want to beat a boss by answering all three questions correctly, so that I can show that I know a lot about air pollution.

> - A player beats the boss only if they answer all three questions correctly.
> - If the player answers all three correctly: the win state is shown.
> - If the player answers one or more questions incorrectly: the player does not beat the boss.
> - After a win, the player returns to the board and can continue moving.

**ABE.07 - Lose to a Boss**  
As a player, I want to lose to a boss by answering at least one question incorrectly, so that the stakes are very high.

> - The player loses the boss battle if they answer one or more questions incorrectly out of the three.
> - Losing occurs as soon as the first incorrect answer is submitted (the player does not need to answer all three to lose).
> - A clear lose state is shown
> - If the player answers question 1 correctly and question 2 incorrectly, they lose; they do not beat the boss.
> - After losing, the player returns to the board and is moved back 5 squares
> - The boss battle is considered finished on loss; the player does not continue answering the remaining questions after an incorrect answer.

**ABE.08 - Learn from boss mistakes**  
As a player, I want to learn about the mistakes I made during boss battle by learning the correct answers to the questions, so that I can keep learning about air pollution.

> - After a boss battle ends (win or lose), the player can see which questions they got wrong.
> - For each incorrect answer, the correct answer is shown.
> - The player can read the feedback before returning to normal gameplay.
> - The feedback is shown only after the battle has ended, not during the three questions.
> - If the player answered all three correctly, either no “mistakes” section is shown or a message like “All correct!” is shown.

**ABE.09 - More Difficult Questions**  
As a player, I want to be asked more difficult questions when I play on the Hard difficulty, so that I can show off my air pollution knowledge.

> - When the game is set to Hard difficulty, questions come from the hard/difficult question set.
> - Hard difficulty applies to blue-card questions and, if applicable, boss-battle questions.
> - The difficulty does not change mid-game unless the player changes settings.
> - The current difficulty (Hard) is visible somewhere on the screen itself.
> - At least one hard question is shown during a typical game when Hard is selected.
> - When the player selects Medium, the game asks questions from the Medium set.
> - Medium difficulty is used for normal blue-card questions and boss questions (if boss questions are used).
> - The difficulty stays on Medium during the game unless the player changes it in settings.
> - The player can clearly tell they are on Medium from what is shown on screen.
> - In a normal Medium game, the player will see at least one Medium question.

**ABE.10 - More Easier Questions**  
As a player, I want to be asked easier questions when I play on the easy difficulty, so that I can still enjoy the game even if I'm still learning about air pollution.

> - When the game is set to Easy difficulty, questions come from the easy question set.
> - Easy difficulty applies to blue-card questions and, if applicable, boss-battle questions.
> - The difficulty does not change mid-game unless the player changes settings.
> - The current difficulty (Easy) is visible somewhere on the screen itself.
> - At least one easy question is shown during a typical game when Easy is selected.

**ABE.11 - Play as the Cyclist**  
As a player, I want to play as the Cyclist, to see the world from their perspective.

> - The player can choose Cyclist on the character selection screen before the game starts.
> - When the Cyclist lands on a “Ride Bike!” path or Green spot, they move +1 extra space automatically.
> - When the Cyclist lands on a “Car/Traffic” Grey spot, they take zero pollution damage.
> - The Cyclist’s abilities are described on the character selection screen and behave as described in the game design.
> - During the game, the avatar and theme reflect the Cyclist.

**ABE.12 - Play as the Scientist**  
As a player, I want to play as the Scientist, to see the world from their perspective.

> - The player can choose Scientist on the character selection screen before the game starts.
> - The player has a “Sensor” button that can be used at most twice per game to reveal the correct answer to a difficult Quiz Card.
> - After two uses, the button is disabled or clearly indicated as unavailable.
> - The Scientist’s ability is described on the character selection screen and works only on quiz/question cards, not on fact-only cards.
> - During the game, the avatar and theme reflect the Scientist.

**ABE.13 - Play as the Ranger**  
As a player, I want to play as the Ranger, to see the world from their perspective.

> - The player can choose Ranger on the character selection screen before the game starts.
> - When the Ranger lands on a Wildfire (grey) spot, they take no pollution damage.
> - When the Ranger lands on a “Plant Trees” (Green) spot, they remove double the usual pollution.
> - The Ranger’s abilities are described on the character selection screen and behave as above in gameplay.
> - During the game, the avatar and theme reflect the Ranger.

**ABE.14 - Board Movement Alone**  
As a player in a single player game, I want to move around the board on my own, so that I can learn about air pollution on my own.

> - When the player chooses Single Player, the game starts with one player.
> - Only that player’s token moves on the board; there are no other human or bot tokens in the same game.
> - The player rolls the dice on their turn and moves their token the rolled number of spaces.
> - The player controls their own turns (e.g. roll, answer cards, complete minigames) without waiting for other players.
> - Turn order is effectively “always my turn” (because it is a single player game).
> - The game can be completed by that single player reaching the end of the board.

### UI / Aesthetics

**UI.01 - Button Hover Effect**  
As a player, I want buttons to change how they look when I hover over them, so that I know what I am about to select.

> - When the player hovers over a button, the button's appearance changes.
> - When the player moves the mouse away (stops hovering), the button returns to its normal/default appearance.
> - The hover effect is consistent across all interactive buttons in the game.
> - The hover effect is clearly visible (enough contrast or change that the player notices).
> - The button text remains readable during the hover state (no clipping or illegibility).
> - If the button is disabled or greyed out, hovering does not change its appearance.

**UI.02 - Button Click Effect**  
As a player, I want buttons to change how they look when I press them, so that I know what actions I've taken.

> - When clicking a selection button causes the button to visually change color. 
> - When clicking on a button repeatedly while it is in the "Pressed", it does not trigger the acion multiple times. 
> - The "Pressed" remains visible until the game processes the action (i.e. when moving to the result screen).
> - The Scientist can only use their special button twice. After the second use, the button should turn grey to show that it cannot be used anymore.
> - When I press a button, the color changes, but the words must still be easy to read.

**UI.03 - 3D graphics on map**  
As a player I want to see 3D graphics of features on the map, so that the world feels rich and engaging.

> - 3D graphics appear for map features (e.g. trees, buildings, factories, mountains, road elements) on the game board.
> - 3D objects are positioned and scaled so they fit the map and look proportional to the board path and tokens.
> - 3D objects are behind UI elements (buttons, pollution meter, menus) and do not block or overlap them.
> - 3D objects do not block the board path or player tokens; gameplay remains readable and playable.
> - 3D objects do not distract from core gameplay (board, tokens, squares); they add atmosphere without obscuring important information.
> - Performance remains acceptable (no noticeable lag or stutter) with 3D objects visible during normal play.
> - The 3D style fits the rest of the game (e.g. matches the overall art style and themes like air quality, pollution).

**UI.04 - Interesting Dice Roll**  
As a player I want to have a pleasent and visually interesting experience rolling dice, so that turns feel exciting.

> - Test that clicking on the dice triggers this animation / interactive animation.
> - Test that the dice actually lands on a valid number after the roll.
> - Make sure that the number that's been rolled is that one that goes into the code to move the player that many spaces.
> - Test the performance of this animation. (making sure it doesn't lag out the rest of the game)
> - Make sure that this animations isn't too jarring and out of place with the rest of the game and UI elements.
> - Test the randomness of this diceroll properly.

**UI.05 - Unify The UI**
As a player I want the UI to feel like one cohesive unit so that it is easy to navigate and interact with.

> - When present the pause button is located in the top right.
> - The main title text uses the same font whenever present in the game.
> - Font color and size is used consistently.

### Main Menu

**MM.01 - Select Difficulty**  
As a player, I want to be able to select between multiple difficulties, so that I can choose how challenging the questions will be.

> - Before or at the start of a game, the player can choose a difficulty (e.g. Easy, Normal, Hard).
> - The chosen difficulty affects question difficulty.
> - The current difficulty is visible somewhere.
> - The chosen difficulty persists for the whole game.
> - The player can start a new game with a different difficulty.
> - At least two difficulty levels are available (e.g. Easy and Hard, or Normal and Hard).

**MM.02 - Pick Character**  
As a player, I want to start the game by selecting what character I will play as, so that I feel connected to who I play as.

> - When the player starts a new game, a character selection step appears.
> - The player can select exactly one character (e.g. Cyclist, Scientist, Ranger) and confirm (e.g. “Start” or “Confirm”).
> - After confirmation, the selected character is the one used in the game.
> - The game does not start without a valid character selected.
> - If the player goes back from character selection to the main menu, they can start again and choose a different character.
> - The chosen character’s abilities (e.g. Cyclist’s Traffic Weaver, Scientist’s Sensor, Ranger’s Fire Resistance) are active during the game as designed.

**MM.03 - Read Special Ability**  
As a player, I want to see the special abilities before I choose, so that I feel connected to my character and gameplay is more fun.

> - On the character selection screen, each character shows its special ability (e.g. text or icon) before the player confirms their choice.
> - The ability description is visible for each character.
> - The player can view all characters' abilities.
> - The ability info is readable and not cut off on the UI.
> - The displayed ability matches the character's actual in-game behaviour.
> - The player cannot start the game without seeing or having access to this ability info.
> - If the player changes their selection (e.g. clicks a different character), the new character's ability is shown.

**MM.04 - Single vs Multiplayer**  
As a player, I want to be able to pick between singleplayer and local multiplayer, so that I can play alone or with classmates.

> - On the main menu (or at game start), the player can choose between Singleplayer and Multiplayer (or "Local Multiplayer").
> - Choosing Singleplayer starts a game with one player (no other human or bot players, unless bots are explicitly added by the player).
> - Choosing Multiplayer starts the multiplayer setup.
> - The game clearly indicates which mode is active during the match.
> - The player cannot accidentally start the wrong mode.
> - If the player returns to the main menu, they can choose a different mode for the next game.
> - In Singleplayer, only one token moves on the board; in Multiplayer, multiple tokens (for human players or bots) are present and take turns.
> - The choice is made before the game starts (not mid-game).

### Multiplayer

**MP.01 - Play with 2 others**  
As a player in a multiplayer game, I want to take turns with up to 2 other players moving around the board, so that I can see how my air pollution knowledge compares to my classmates.

> - The player can start a multiplayer game with 2 or 3 human players.
> - Players take turns in a defined order.
> - On each player's turn, only that player rolls the dice and moves, other players wait.
> - Each player has a separate token and position on the board.
> - Turn order repeats until the game ends.
> - The current player is clearly indicated.
> - A player cannot roll or move when it is another player's turn.
> - When a player lands on a blue square and draws a card, only that player sees and answers the question; the other players wait.
> - The game supports exactly 2 or 3 human players.

**MP.02 - Play with 2 bots**  
As a player in a multiplayer game, I want to take turns with up to 2 other bots moving around the board, so that I can learn about air pollution individually but still in a competitive environment.

> - The player can start a multiplayer game with 2 or 3 total players, where 1 or 2 are bots.
> - Bots take turns in the same order as human players.
> - When it is a bot's turn, the bot rolls the dice and moves automatically (without any human's input).
> - Each bot has its own token, position, and pollution score.
> - When a bot lands on a blue square, the bot answers the question automatically.
> - Bots apply the same board rules as the humans.
> - The game supports 1 human + 1 bot or 1 human + 2 bots.
> - The current turn (human or bot) is clearly indicated.
> - Bot turns complete within a reasonable time so the game does not feel stuck.

**MP.03 - Win by lower AQHI**  
As a player in a multiplayer game, I want to win by having a lower pollution score then my opponents, so that I can show off my air pollution knowledge.

> - In multiplayer, the player with the lowest pollution score when the game ends is the winner.
> - "Game ends" means at least one player has reached the final space (38) and the round/end condition is applied.
> - If two or more players tie for the lowest pollution score, the winner is defined.
> - The pollution score is the Lung Meter / pompom count (0–10); lower is better.
> - Pollution scores are updated correctly for each player as they land on green, grey, and blue squares.
> - The winner is clearly announced at the end of the game.
> - All players can see final scores (or at least who won) at game end.
> - The win condition uses pollution score only (or as the primary tiebreaker), not board position alone.

**MP.04 - Don't see repeat questions**  
As a player in a multiplayer game, I do not want to see any questions that my opponents have already seen, so that I cant cheat by already knowing the answers.

> - (Assuming the card pool is not exhausted) Each blue card (fact or question) is shown to only one player during the game, the one who landed on the blue square.
> - If the blue card pool is exhausted, the game re-adds in all of the blue game cards that were present at the start of the game.
> - Questions are drawn fairly (random from unseen pool) so no player is systematically favored.

### Settings

**SET.01 - Adjust BGM Volume**  
As a player, I want to adjust background music volume, so that it is comfortable for me.

> - A background music volume control exists in Settings.
> - Changing the setting immediately adjusts the background music volume in-game.
> - Setting the volume to 0 silences the background music completely.
> - If master volume (SET.02) is set to 0, background music is also muted regardless of this setting.
> - The chosen volume is saved and persists after the player starts a new game.
> - If the game is restarted (e.g. new game), the background music does not restart from scratch.

**SET.02 - Adjust Master Volume**  
As a player, I want to be able to adjust the master volume, so that I can quickly control overall sound levels.

> - A master volume control exists in Settings.
> - Changing the master volume affects all game sounds (background music, sound effects).
> - Setting master volume to 0 or mute silences all audio in the game.
> - The master volume acts as a multiplier or cap on other volume settings.
> - The chosen volume is saved and persists after the player closes and reopens the game.
> - Adjusting master volume does not reset or change the individual volume settings.

**SET.03 - Adjust Sound FX Volume**  
As a player, I want to be able to adjust the sound effect volume, so that sound effects are not distracting or too quiet.

> - A sound effects volume control exists in Settings.
> - Changing the setting immediately (or after confirmation) adjusts the volume of all sound effects.
> - Setting the volume to 0 or mute silences all sound effects.
> - If master volume (SET.02) is set to 0, sound effects are also muted regardless of this setting.
> - The chosen volume is saved and persists after the player closes and reopens the game.
> - Changing this setting does not affect background music volume.

### Sound

**S.01 - Sound of Nearby Things**  
As a player, I want to hear sound effects of things near me on the board, so that I can increase my immersion.

> - Sound effects play for objects or events near the player's current position (e.g. factory hum near an industrial zone, wind near open areas).
> - Sounds are louder when the player is closer to the source and quieter when farther away.
> - Muting sound effects in settings also mutes these positional sounds.
> - The sounds are appropriate for the board locations.
> - The positional sound effects do not overpower important audio.

**S.02 - Background Music**  
As a player, I want background music during gameplay, so that the experience feels lively.

> - Background music starts playing when the game begins.
> - The music loops or continues throughout gameplay.
> - The music starts at a comfortable default volume.
> - By default, the music is quiet enough to still hear sound effects.
> - The music can be muted or adjusted in settings.

**S.03 - Confirmation Notes**  
As a player, I want to hear a confirmation noise when I press a button, so that I know my input was registered.

> - When any interactive button is pressed, a confirmation sound plays.
> - The sound plays immediately after the button press.
> - If the player presses multiple buttons quickly, the sounds do not overlap in a jarring way.
> - The confirmation sound is affected by the sound effects volume setting.
> - If sound effects are muted, the confirmation sound does not play.
> - The sound is consistent across all buttons.

### Minigames

**MG.01 - Stay Inside & Clean The Air**  
As a player, I want to play a minigame where I get rid of smoke pretending to be the wind, so that I interact physically with pollution.

> - When the player lands on a green square that triggers "Stay Inside and Clean the Air", the minigame opens and can be started.
> - The player can reduce smoke by tapping smoke clouds directly; tapping the background does not clear smoke.
> - Smoke and wind animations play and are visible during the minigame.
> - The minigame has a clear win condition (clear enough smoke within the time limit), and the game recognizes it.
> - When time is up (or the win condition is met), an end/result page is shown with outcome feedback.
> - Selecting Exit Minigame from the intro or end page closes the minigame and returns to the board scene without freeze/hang.
> - The theme (wind clearing smoke/pollution) is clear in visuals and interactions.

**MG.02 - Planting Trees**  
As a player, I want to play a minigame where I plant trees, so that I feel like I am helping the environment.

> - When the player lands on the green square for Plant Trees, the Plant Trees minigame is displayed.
> - The player can interact with the planting area to plant trees.
> - Planting trees shows visible feedback in the garden.
> - The minigame ends after 5 seconds.
> - If the player exits early or closes the result page, the minigame closes and control returns to the main game.
> - The pollution reward is applied when more than 10 taps gives -2
> - The pollution reward is applied when 1 to 10 taps gives -1
> - The pollution reward is applied when 0 taps gives 0
> - The tree-planting/environment theme is clear from the minigame text and visuals.

**MG.03 - Public Transportation**   
As a player, I want to move between transportation lanes in a minigame, so that I can choose more eco-friendly transportation options and reduce pollution.

> - When the player lands on a green square, the Public Transportation minigame is displayed.
> - The player sees three transportation lanes: Car, Bus, and Metro.
> - The player can move between lanes during the minigame using keyboard input, and swipe input on touch devices.
> - If the player finishes the minigame in the Metro lane, the pollution score decreases by 2.
> - If the player finishes the minigame in the Bus lane, the pollution score decreases by 1.
> - If the player finishes the minigame in the Car lane, the pollution score does not decrease.
> - If the player hits an obstacle, the minigame shows end feedback with no pollution reduction.
> - After viewing the end feedback, the player can dismiss the minigame and return to the board.

**MG.04 - Using Clean Energy**   
As a player I want to learn clean energy sources by playing a minigame in which I must choose the correct clean energy methods.

> - The user first sees a start page, then is prompted with three energy sources after clicking Begin.
> - The user may select an energy source by clicking one of the three choices during gameplay.
> - If the user selects the clean source, the end page shows a success result and positive pollution reduction.
> - If the user selects a dirty source, the end page shows a failure result and negative pollution reduction.
> - A result sound plays immediately on success or failure selection.
> - The user’s pollution decreases when they finish from the end page after a correct choice.
> - The user’s pollution increases when they finish from the end page after a wrong choice.

**MG.05 - Using a Mask**  
As a player, I want to choose the correct mask to wear by dragging it onto a face, so that I learn how protecting myself from polluted air works.

> - The user sees a face in the center and 3 mask options (1 correct, 2 incorrect).
> - The user can drag and drop a mask onto the face.
> - Dropping an incorrect mask shows feedback (Incorrect. Try again. or Out of attempts.) and the wrong mask resets instead of attaching.
> - Dropping the correct mask snaps it to the face, shows success feedback, waits about 2 seconds, then shows the end page.
> - The minigame ends on success, attempts running out, or timer expiry (8s), then shows the end page.
> - From the end page, Exit Minigame returns to the main game.
> -  Pollution outcome is applied when exiting from the end page: success reduces pollution (+1 reduction), failure/time-up gives 0 reduction.

**MG.06 - Riding a Bike**   
As a player, I want to play a minigame where I ride a bike, so that I learn how biking reduces air pollution and helps the environment.

> - When the player lands on a green square, the Ride a Bike minigame opens with a start page.
> - The player can tap to pedal/control the bike
> - Visual feedback shows the bike moving or pedaling animation.
> - The minigame ends when the player travels 50 meters or when time runs out.
> - After a run ends, an end page is shown; selecting Exit Minigame closes it and returns to the main game.
> - If the player exits early from start/gameplay (Exit), the minigame closes and returns to the main game with no reward applied.
> - Pollution reward is applied on end-page exit based on distance: 50m+ is 2 points, 25m to 49m is 1 point, <25m is 0 point.
> - The clean transportation/biking theme is clear in visuals and text.
> - Performance feedback is displayed at the end "Great job! You biked X meters!".

**MG.07 - Save Energy**   
As a player I want to save energy by turning off all of the lights left on in a building, so that I can practice saving the environment.

> - The user gains 1 point for each second a light is left on
> - The light turns off whenever the user presses it.
> - Randomly, the lights are turned on by NPCs that run around between the rooms.
> - If the user has 25 or more points at the end, they gain 1 pollution point.
> - If the user has less than 25 points at the end, they lose 1 pollution point.

**MG.08 Recycling**   
As a player, I want to sort trash by swiping each item into the correct recycling bin, so that I can learn proper recycling habits and reduce the AQHI score.

> - When the player triggers the Recycling Sorting minigame, the minigame screen is displayed with four recycling bins (two on the left and two on the right).
> - A single item is displayed in the center of the screen at the start of the minigame.
> - The player can drag and drop the item into one of the recycling bins.
> - After an item is sorted, the next item appears in the center until all items are completed.
> - If the player swipes an item into the correct recycling bin, the score increases by 1.
> - If the player swipes an item into the wrong bin, the score decreases by 1.
> - When all items are sorted, the minigame ends and the final score is displayed.
> - Based on the final score:
>> - Score ≥ 7 → AQHI decreases by 2
>> - Score ≥ 4 → AQHI decreases by 1
>> - Score < 4 → no change to AQHI

**MG.09 Biofilters**   
As a player, I want to build a biofilter by selecting filter layers in the correct order, so that I can learn how natural filtration systems clean polluted water and reduce pollution in the game.

> - When the player lands on the Biofilter trigger square (green-square event), the Biofilter minigame opens.
> - The intro screen shows the minigame title, kid-friendly rules text, and a Start button.
> - After pressing Start, the player can choose filter layers: Gravel, Sand, Charcoal, Plants.
> - The selected-layers display and feedback update as the player adds layers, and the player can press Run Filter to finish.
> - A timer and progress info are visible during gameplay and update continuously.
> - When time runs out (or Run Filter is pressed), the result screen shows how many layers were in the correct position and the pollution reduction earned.
> - On the result screen, pressing Continue closes the minigame and applies the earned pollution reduction.

**MG.10 Composting**   
As a player, I want to sort waste into the correct bin during a composting minigame, so that I can learn which items belong in compost, recycling, or landfill and reduce pollution in the game.

> - When the player lands on board square 45 (a green square), the Composting minigame opens.
> - The intro screen shows the minigame title, kid-friendly rules text, and a Start button.
> - When the player presses Start, one waste item appears and the player can choose Compost, Recycle, or Landfill.
> - When the player selects a bin, the game checks the choice, shows feedback, and then loads the next item.
> - A timer and score are visible during gameplay and update as the minigame continues.
> - When time runs out, the minigame ends and the result screen shows the number of correct answers and the pollution reduction earned.
> - The intro screen has no exit action (Start-only flow).
> - When the player presses Continue on the result screen, the earned pollution reduction is applied.


### Admin

**AD.01 - Update Blue Fact Cards**  
As an admin, I want to update the facts in the blue cards, so that the information stays correct and updated.

> - The admin is logged into the system.
> - The user lands on a blue spot and picks a card.
> - The card can either have a fact or a question.
> - The admin can change any factual inaccuarcy in the card.
> - The admin can also change/edit any question from the card.
> - After making any changes, the changes should be saved and visible.

**AD.02 - Update Blue Question Cards**  
As an admin, I want to update the questions in the blue cards, so that we can prevent any redundacy with the questions.

> - The admin is logged into the system.
> - The user lands on a blue spot and picks a card.
> - The card can either have a fact or a question.
> - The admin can also change/edit any question from the card.
> - After making any changes, the changes should be saved and visible.

**AD.03 - Join Session with Room Code**  
As an admin, I want to make all the kids in a class join a session with a room code, so that the class can learn together.

> - An admin can create or start a session and receive a room code.
> - The admin can share the room code so that students in their class can join the same session.
> - A student who enters the correct room code is added to the session and can start or join a game.
> - A student who enters an invalid or expired room code receives an error message and is not added to the session.
> - All students who join using the same valid room code are placed in the same session/room.
> - The admin can see how many students have joined the session.
> - The room code is unique for that session.
> - Once the admin starts the session, students can join until the admin closes join or the session ends.

**AD.04 - View Current Games**  
As an admin, I want to see the current games going on in my room, so that I can make sure everyone is staying on task.

> - The admin has a view that shows active games in their room.
> - Only games in the admin’s current room/session are shown.
> - For each active game, the admin can see at least: number of players, game status.
> - The list updates when new games start or existing games end.
> - If no games are active, the view shows an empty state.
> - The admin can distinguish their room from other rooms
> - The view is accessible only to admins.

**AD.05 - View Current Scores**  
As an admin, I want to see all the scores of the players who are playing right now in my room, so that I can see who is doing the best.

> - The admin has a view showing live scores of players currently in games in their room.
> - For each active player, the admin sees at least: player name/identifier and current pollution score.
> - Scores update as players land on green/grey squares, answer blue cards, etc.
> - Only players in active games in the admin’s room are shown.
> - If a game ends, those players are removed from or clearly marked as finished in the live view.
> - If no one is playing, the view shows an empty state (e.g. “No active players”).
> - The admin can see which game each player belongs to (if multiple games run at once).
> - The view is restricted to admins for that room.

**AD.06 - See Final High Scores**  
As an admin, I want to see a leaderboard of the highest scores at the end of a session, so that I can see who did the best.

> - When a session ends, the admin can view a leaderboard for that session.
> - The leaderboard lists players from that session, ordered by best performance.
> - For each player, the leaderboard shows at least: player name/identifier and final score.
> - Ties are handled consistently.
> - The leaderboard reflects all players who completed at least one game in that session.
> - The leaderboard is available only after the session has ended.
> - The admin can identify which session/room the leaderboard belongs to.
> - The leaderboard remains available for viewing after the session ends (like a historical view).

### Accessibility

**ACC.01 - Increase Font Size**  
As a player, I want to be able to increase the font size of the text, so that I can read the facts and questions comfortably.

> - Font Size option exists in Settings/Accessibility.
> - Changing it updates all game text (cards, menus, results).
> - Large size doesn’t clip/overlap text; text remains readable.
> - Setting saves after restart.
> - At least two distinct sizes are available.

**ACC.02 - Text to Speech**  
As a player, I want to press a speaker button to hear facts and questions read aloud, so that I can play without relying on reading.

> - Speaker button appears on fact/question cards.
> - Press plays TTS for the current card’s text.
> - Press again stops (or restarts) without overlapping audio.
> - Switching cards reads the new card (old audio stops).

**ACC.03 - Adjust difficulty dynamically**  
As a player, I want the game to adjust difficulty based on my performance, so that the game stays challenging but not frustrating.

> - The game tracks the player’s recent performance.
> - When the player answers most of the last N questions correctly, the difficulty increases.
> - When the player answers most of the last N questions incorrectly, the difficulty decreases.
> - When the player’s performance is in the middle, the difficulty does not change.
> - The adjustment is applied during the game.
> - The game does not switch difficulty so often that it feels random.
> - The player is not required to change any settings; adjustment is automatic.

## MoSCoW

| Must Have | Should Have | Could Have | Nice to Have | Won't Do |
| ------ | ------ | ------ | ------ | ------ |
| GBM.01 - Roll Dice Movement       | GBM.08 - Turn Camera Follow               | GBM.12 - Green Square Fact          | ABE.02 - Start Boss Battles  | AD.01 - Update Blue Fact Cards    |
| GBM.02 - Draw Blue Card           | GBM.11 - Wrong Answer Backstep            | MM.02 - Pick Character              |       | AD.02 - Update Blue Question Cards |
| GBM.03 - Air-Pollusion Facts      | ABE.01 - Minigame on Green Square         | ABE.05 - Fight a Boss               |       | AD.03 - Join Session with Room Code  |
| GBM.04 - Blue Trivia Questions    | ABE.09 - More Difficult Questions         | ABE.06 - Beat a Boss                |       | AD.04 - View Current Games   |
| GBM.05 - Grey Square Penalty      | ABE.10 - More Easier Questions            | ABE.07 - Lose to a Boss             |       | AD.05 - View Current Scores       | 
| GBM.06 - Green Square Reward      | MM.01 - Select Difficulty                 | ABE.08 - Learn from boss mistakes   |       | AD.06 - See Final High Scores     | 
| GBM.07 - Visual Pollution Score   | MM.03 - Read Special Ability              | ABE.11 - Play as the Cyclist        |       | S.01 - Sound of Nearby Things |
| GBM.09 - Choose Trivia Answer     | MM.04 - Single vs Multiplayer             | ABE.12 - Play as the Scientist      |       | ABE.03 - Random Weather Effects |
| GBM.10 - Final Score Display      | MP.01 - Play with 2 others                | ABE.13 - Play as the Ranger         |       | MP.02 - Play with 2 bots |      
| GBM.13 - Learn from Mistakes      | MP.03 - Win by lower AQHI                 | UI.03 - 3D graphics on map          |       | ACC.03 - Adjust difficulty dynamically |
| ABE.14 - Board Movement Alone     | MP.04 - Don't see repeat questions        | UI.04 - Interesting Dice Roll       |       | ABE.04 - Trigger Airflow Combo |
| UI.01 - Button Hover Effect       | SET.01 - Adjust BGM Volume                | S.02 - Background Music             |       |
| UI.02 - Button Click Effect       | SET.02 - Adjust Master Volume             | MG.01 - Stay Inside & Clean The Air |       |
| S.03 - Confirmation Notes         | SET.03 - Adjust Sound FX Volume           | MG.02 - Planting Trees              |       |
|                                   | ACC.01 - Increase Font Size               | MG.03 - Public Transportation       |       |
|                                   | ACC.02 - Text to Speech                   | MG.04 - Using Clean Energy          |       |
|                                   | UI.05 - Unify the UI                      | MG.05 - Using a Mask                |       |
|                                   |                                           | MG.06 - Riding a Bike               |       |
|                                   |                                           | MG.07 - Save Energy                 |       |
|                                   |                                           | MG.08 Recycling                     |       |
|                                   |                                           | MG.09 Biofilters                    |       |
|                                   |                                           | MG.10 Composting                    |       |


## Similar Products

We looked at a mix of existing educational and environmental games as reference points for both content and gameplay style.

#### [Geothermal Energy Game](https://energyadventure.itch.io/geothermal-energy-game)
An educational web-based game that teaches players about geothermal energy through short lessons and interactive quizzes.

- Uses multiple-choice quizzes to test understanding  
- Progresses through content in small sections  
- Keeps the focus on learning through quizzes  

#### [Discovering Renewable Futures](https://renewable-futures-game.itch.io/discovering-renewable-futures)
An educational game that introduces renewable energy concepts in an interactive way.

- Strong visual design  
- Clear progression through topics  
- Uses layout and visuals to support learning  

#### [Smog: The Air Pollution Game](https://www.gamesforchange.org/game/smog-the-air-pollution-game/)
A board game focused on air pollution and environmental decision-making.

- Turn-based board gameplay  
- Pollution changes based on player actions  
- Shows cause-and-effect clearly  

#### [AQHI Fortune Teller Game](https://www.canada.ca/en/environment-climate-change/services/air-quality-health-index/education-tools.html)
An educational activity designed to help players understand the Air Quality Health Index.

- Introduces AQHI levels in a simple way  
- Connects air quality to health impacts  
- Easy to understand for younger audiences  

#### [The Clean Air Game](https://www.cleanairgame.com/)
A classroom-based educational game that teaches players about air pollution and clean air actions.

- Learning through gameplay  
- Rewards positive environmental actions  
- Shows consequences of pollution choices  

#### [Protecting Gaia: A Battle for Better Air Quality](https://www.cleanairpartnership.org/protecting-gaia/)
A board game centered around improving air quality through different roles and challenges.

- Role-based gameplay  
- Uses challenge and event cards  
- Treats air quality as a shared system  

#### [AR Biosphere Gaming App](https://cmput401.ca/projects/d04adf5c-bfcf-45ed-8b8c-1eb9ec6f86df)
A mobile game that lets users interact with environmental content using mini-games and exploration.

- Short mini-games  
- Trivia and interactive challenges  
- Learning through interaction rather than text  


## Open-Source Projects
Potentially useful open source projects to aid in development.  

- [TTS Plugin](https://github.com/unitycoder/UnityRuntimeTextToSpeech/wiki/Alternative-TTS-plugins-for-Unity): In our initial project meetings we discussed some potential accessibility features like Text to speech. Being able to have the card audio read aloud to children could assist with ease of use. This is a TTS plugin that could, during runtime, generate audio narration of text.
- [Blender](https://www.blender.org/): A nice to have of the project is 3d assets. Blender is an open-source tool to work 3d models, effects, and animations. 
- [SketchFab](https://sketchfab.com/features/free-3d-models): SketchFab is a marketplace for 3d assets. There are plenty of free use assets that we could possibly use for our 3D models.
  
## Technical Resources

- [Unity Documentation](https://docs.unity.com/en-us)
- [Unity Testing Framework Documentation](https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/index.html)
- [itch.io FAQ](https://itch.io/docs/general/faq)
- [Pixabay](https://pixabay.com/sound-effects/) - Royalty-Free Sound Effect Library
- [Icons8] (https://icons8.com/)

## Detailed list of technologies

- [Unity](https://unity.com/) - Game engine used to build the game
- [itch.io](https://itch.io) - Hosting website used to host the game
