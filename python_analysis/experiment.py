import re

from maikol_utils.print_utils import print_warn
from maikol_utils.file_utils import list_dir_files # Mi librería personal :p

class GAExperiment:
    def __init__(self, path: str):
        self.path = path

        # Las clases de LC patrocinan este regex ;)
        # "GA_Log_20251016_233329-Aleo_Simple-Pop_20000-Mut_0.15-Pieces_10-Seed_42.log",
        pattern = r"GA_Log_(\d{8}_\d{6})-Aleo_([\w-]+)-Pop_(\d+)-Mut_(\d+\.?\d*)-Pieces_(\d+)-Seed_(\d+)"
        match = re.search(pattern, path)
        if match:
            self.date = match.group(1)
            self.agent = match.group(2)
            self.population = int(match.group(3))
            self.mutation_rate = float(match.group(4))
            self.pieces = int(match.group(5))
            self.seed = int(match.group(6))
        else:
            print_warn(f"Invalid experiment folder name: {path}")
            self.agent = None
        
    def __repr__(self):
        # return (f"Experiment(date={self.date}, agent={self.agent}, population={self.population}, "
        #         f"mutation_rate={self.mutation_rate}, pieces={self.pieces}, seed={self.seed})")
        return (f"GEN_{self.agent}-POP_{self.population}-MUT_{self.mutation_rate:.0%}-NP_{self.pieces}")

    def load_log(self):
        gens, fits = [], []
        # with open(self.path, "r", encoding="utf-8") as f:
        with open(self.path, "r", encoding="utf-8", errors="replace") as f:
            for line in f:
                if "Generation" in line:
                    parts = line.strip().split(". ")
                    generation = int(parts[0].split(": ")[1])
                    best_fitness = float(parts[1].split(": ")[1])
                    gens.append(generation)
                    fits.append(best_fitness)
        return gens, fits
    
    def load_last_movements(self):
        last_movement = ''
        pieces = []
        # with open(self.path, "r", encoding="utf-8", ) as f:
        with open(self.path, "r", encoding="utf-8", errors="replace") as f:
            for line in f:
                if 'Bag pieces' in line:
                    pieces = line.split("Bag pieces:")[1].split()

                if 'Genotype' in line:
                    last_movement = ''
                    
                last_movement+=line

        return last_movement, pieces


class SAExperiment:
    def __init__(self, path: str):
        self.path = path

        # Las clases de LC patrocinan este regex ;)
        # "SA_Log_20251018_194113-Aleo_SwapDoble-Pieces_30-Tabu_1000-UpdFact_0.005-Seed_42.log",
        # "SA_Log_20251018_130906-Aleo_Simple-Pieces_10-Tabu_100-UpdFact_5E-05-Seed_42.log"
        pattern = r"SA_Log_(\d{8}_\d{6})-Aleo_([\w-]+)-Pieces_(\d+)-Tabu_(\d+)-UpdFact_([\d.]+(?:E-?\d+)?)-Seed_(\d+)"
        match = re.search(pattern, path)
        if match:
            self.date = match.group(1)
            self.agent = match.group(2)
            self.pieces = int(match.group(3))
            self.tabu = int(match.group(4))
            self.update_factor = float(match.group(5))
            self.seed = int(match.group(6))
        else:
            print_warn(f"Invalid experiment folder name: {path}")
            self.agent = None
        
    def __repr__(self):
        return (f"GEN_{self.agent}-NP_{self.pieces}-TAB_{self.tabu}-UPF_{self.update_factor}")

    def load_log(self):
        gens, fits, temps = [], [], []
        gens_best, fits_best = [], []
        generation = None
        with open(self.path, "r", encoding="utf-8", errors="replace") as f:
            for line in f:
                if "Gen:" in line and "Updated:" in line:
                    # [2025-10-18 19:13:04] Gen: 4466. Updated: -164.6786. Temp: 10.71466
                    # Extract values using split on known delimiters
                    gen_part = line.split("Gen:")[1].split(".")[0].strip()
                    updated_part = line.split("Updated:")[1].split(".")[0].strip()
                    # Get everything after "Temp:" to the end of line
                    temp_part = line.split("Temp:")[1].strip()
                    
                    generation = int(gen_part)
                    fitness = float(updated_part)
                    temp = float(temp_part)
                    
                    gens.append(generation)
                    fits.append(fitness)
                    temps.append(temp)

                if "Best score:" in line and generation is not None:
                    # [2025-10-18 19:13:04] Best score: -155.9286 Genotype:
                    score_part = line.split("Best score:")[1].split("Genotype")[0].strip()
                    best_score = float(score_part)
                    gens_best.append(generation)
                    fits_best.append(best_score)
        return gens, fits, temps, gens_best, fits_best

    def load_last_movements(self):
        last_movement = ''
        pieces = []
        with open(self.path, "r", encoding="utf-8", errors="replace") as f:
            for line in f:
                if 'Bag pieces' in line:
                    pieces = line.split(":")[1].split()

                if 'Genotype' in line:
                    last_movement = ''
                    
                last_movement+=line

        return last_movement, pieces




def load_experiments(CONFIG):
    experiments_ga, n = list_dir_files(CONFIG.raw_path_ga, recursive=True)
    experiments_show, n = list_dir_files(CONFIG.show_path_ga, recursive=True)
    experiments_sa, n = list_dir_files(CONFIG.raw_path_sa, recursive=True)
    GA_experiments = [exp for exp in experiments_ga + experiments_show if 'GA' in exp]
    SA_experiments = [exp for exp in experiments_sa if 'SA' in exp]
    print(f"Found {n} files in {CONFIG.raw_path_ga} & {CONFIG.raw_path_sa}:")
    print(f"Found {len(GA_experiments)} for GA")
    print(f"Found {len(SA_experiments)} for SA")

    GA_exp_list = []
    for exp in GA_experiments:
        GA_exp_list.append(GAExperiment(exp))
    print(f"Loaded {len(GA_exp_list)} valid experiments.")


    SA_exp_list = []
    for exp in SA_experiments:
        SA_exp_list.append(SAExperiment(exp))
    SA_exp_list = [exp for exp in SA_exp_list if exp.agent is not None]
    print(f"Loaded {len(SA_exp_list)} valid experiments.")

    return GA_exp_list, SA_exp_list