# TetrisSolver
This project contains an implementation of a modern version of the game Tetris. This implementation is used alongside the implementation of two MetaHeuristic algorithms: A Genetic Algorithm (GA) and an Simulated Anneling (SA). 

This algorithms try to accomplish the objective of finding the best position in which a certain number of pieces should be aranged to minimize the space ocupied in the board. This is done by seeing a solution as a sequence of movements for each peace. At the end, the final state of the board is evaluated with a fitness functions, that stablishes how good or bad this solution is.

Over many iterations, this algorithms achieve a sequence of movements that complete the final objetive. This is also a slow approach of how a computer could play this game autonomously, but at the end we accomplish the objetive of finding good solutions to the problem imposed.

![tetris_gif](readme_images/Long.gif)

# Paper of the project
The paper of this project can be found at [this repo](https://github.com/MiquelGomezCorral/TetrisSolverPaper). There you will find the PDF and latex code to generate the paper.

The implementation details, experiments done and decisions made are very well explained there.

# How to use this Repository

1. Clone repo.
2. Install Unity hub and Unity editor
   1. Version 6000.2.7f2 was the last used in the develop of the project
3. Open or add Repository with Unity Hub
4. Open the editor for the project
   1. Go to scenes
   2. TMHSolver

From there you will se in the object list two elements: 'GA Solver' and 'SA Solver'.

* Both object have a tick for enabling execution of the algorithm: `execute computation`
* When all the hyperparameters (algorithm and Heuristics) are set as needed, just press the play button and the selected algorithm will run
  - Note: If both are selected both will run at the same time, which may cause conflicts in the solution viewer

### Solution viewer

The project has, with the implemented game, a solution visualizer. This object allow the algorithms to show every few iterations which is the best solution found until that very moment.

Also, when a certain solution is found and want to be replayed, the project contains a [notebook](python_analysis/parse_movements.ipynb) that can parse the string generated at the logs of the execution to recreate and object in C# that can be used to recreate the full sequence.

### Solution analysis
At python_analysis folder, code is implemented to parse the logs of the executions and plot how the different experiments have influenced the solutions accomplish.
