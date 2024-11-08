# Main Game Components

## AlbertAgent.cs

**Function**: Defines the behavior of the agent Albert, including basic movements (walking, turning, jumping), environmental perception through raycasts at different heights, and interaction with objects.

- **Perception**: Albert uses raycasts (detection rays) at three different heights (low, medium, and high) to perceive his surroundings. This allows him to identify the distance and type of nearby objects, such as walls, floors, doors, and targets (pressure plates).
- **Rewards and Penalties**: Albert is rewarded for desirable actions, like reaching pressure plates quickly, moving toward the exit, and escaping the room. Penalties are applied for undesirable behaviors, such as falling, colliding with walls, or touching the floor.
- **Memory**: The agent maintains a history of observations to improve its decision-making, allowing it to consider past information when determining future actions.

## TargetPlatform.cs

**Function**: Represents pressure platforms that, when activated by Albert, change color and move down. These platforms interact with the door, causing it to open when activated.

- **Interaction**: When Albert is in contact with the platform, it activates, changes color to indicate its state, and triggers the door to open. Upon leaving the platform, after a short waiting period, the platform returns to an inactive state, reverting its color and closing the door.

## PhaseTimer.cs

**Function**: Manages the different phases of the game, controlling time, spawn points (where Albert appears), checkpoints, and user interface (UI) updates.

- **Phases**: Each phase has a name, time limit, multiple spawn points, and a checkpoint. Albert must reach the checkpoint before time runs out to advance to the next phase.
- **UI**: Displays the remaining time and the generation count (Albert’s attempts) so the player can track progress.

## Door.cs

**Function**: Controls the movement of the door that Albert must reach to complete the level. The door can open and close, moving vertically based on interactions with the pressure platforms.

- **Movement**: The door rises when activated and lowers when deactivated, using coroutines to smooth the movement.

## Agent Training (Albert)

- **Learning Algorithm**: Albert is trained using Reinforcement Learning, where he learns through interactions with the environment, receiving rewards or penalties based on his actions.
- **Observations**: In each interaction, Albert collects observations about the environment, including:
  - **Distances**: Measures the distance to different objects detected by raycasts.
  - **Object Types**: Identifies the type of object being detected (e.g., wall, floor, door, target).
  - **Physical State**: Information about his own speed and whether he is grounded.
- **Actions**: Based on the observations, Albert decides his actions, such as moving forward, turning, or jumping, with the goal of maximizing received rewards.

### Rewards

Rewards are structured to encourage Albert to:

- **Efficiency**: Reach pressure plates quickly.
- **Direction**: Move toward the exit.
- **End Goal**: Escape the room.

### Penalties

Penalties are applied to discourage undesirable behaviors, such as:

- **Falling**: Penalty for falling.
- **Collision**: Penalty for colliding with walls or touching the floor.
- **Time**: Small continuous penalties to encourage quicker actions.

## Agent Objectives and Expectations

- **Efficient Navigation**: Albert should learn to navigate the environment efficiently, avoiding obstacles and using pressure platforms to open doors leading to the exit.
- **Object Interaction**: Strategically use pressure platforms to open doors and advance to the next phase.
- **Time Management**: Reach checkpoints before time runs out to progress in the game.
- **Adaptation**: Adapt to different phases with multiple spawn points and variations in the environment, refining strategies to maximize rewards and minimize penalties.

## Agent Behavior

- **Environmental Perception**: Uses raycasts at different heights to create a comprehensive representation of the surrounding environment, enabling him to detect and categorize objects and obstacles.
- **Decision Making**: Based on observations, Albert uses trained neural networks to decide the best action to take in each situation, aiming to maximize accumulated rewards.
- **Memory and Learning**: Maintains a history of observations to consider past context when deciding future actions, improving strategy efficiency over time.
- **Reaction to Changes**: Capable of reacting to environmental changes, such as the activation or deactivation of pressure platforms and the resulting movement of doors.

## Game Progression

### Initial Phase:

- Albert is positioned at a random spawn point.
- He must explore the environment using his movement and perception abilities.

### Pressure Platform Interaction:

- When encountering a pressure platform, Albert should interact with it to trigger the door to open.
- Activation changes the platform’s color and moves the door, allowing progress.

### Checkpoint and Phase Advancement:

- Reaching the checkpoint within the allotted time advances Albert to the next phase.
- Each new phase may introduce additional challenges, such as more obstacles or different spawn point and checkpoint arrangements.

### Final Goal:

- Continue advancing through successive phases until completing all stages and reaching the final goal, which may involve escaping a room or completing a complex environment.

## Player Interaction

- **Visual Feedback**: The UI displays information such as remaining time and generation count, allowing the player to monitor Albert’s progress.
- **Interactive Elements**: Objects like pressure platforms and doors respond to Albert’s actions, creating a dynamic environment that reacts to the agent's decisions.
