# TetrisSolver

A Tetris engine-bot-agent like to play the old Tetris game


# How to use

1. Clone repo.
2. Install Unity hub and Unity editor
   1. Version 6000.2.7f2 was the last used in the develop of the project
3. Open or add Repository with Unity Hub
4. Open the editor for the porject
   1. Go to scenes
   2. TMHSolver

From there you will se in the object list two elements: 'GA Solver' and 'SA Solver'.

* Both object have a tick for enabling execution of the algorithm: `execute computation`
* When all the hyperparameters (algorithm and Heuristics) are set as needed, just press the play button and the selected algorithm will run
  - Note: If both are selected both will run at the same time, which may cause conflicts in the solution viewer

### Solution viwer
The project has, with the implemented game, a solution visualizer. This object allow the algorithms to show every few iterations which is the best solution found until that very moment.