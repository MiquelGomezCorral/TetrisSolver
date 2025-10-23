import plotly.graph_objects as go
from plotly.subplots import make_subplots
import pandas as pd
import re


# =======================================================================================================
#                                               BOX PLOTS
# =======================================================================================================
def parse_GA_params(exp_list):
    """Parse GA experiment parameters from the name"""
    data = []
    for score, exp in exp_list:
        name = repr(exp)
        # Extract GA parameters
        gen_match = re.search(r'GEN_([\w]+)', name)
        pop_match = re.search(r'POP_(\d+)', name)
        mut_match = re.search(r'MUT_([\d]+%)', name)
        np_match = re.search(r'NP_(\d+)', name)
        
        data.append({
            'score': score,
            'GEN': gen_match.group(1) if gen_match else 'Unknown',
            'POP': int(pop_match.group(1)) if pop_match else 0,
            'MUT': mut_match.group(1) if mut_match else '0%',
            'NP': int(np_match.group(1)) if np_match else 0,
            'exp': exp
        })
    
    return pd.DataFrame(data)

def parse_SA_params(exp_list):
    """Parse SA experiment parameters from the name"""
    data = []
    for score, exp in exp_list:
        name = repr(exp)
        # Extract SA parameters - format: GEN_SwapDoble-NP_30-TAB_50000-UPF_0.05
        gen_match = re.search(r'GEN_([\w]+)', name)
        np_match = re.search(r'NP_(\d+)', name)
        tab_match = re.search(r'TAB_(\d+)', name)
        upf_match = re.search(r'UPF_([\d.]+(?:e-?\d+)?)', name, re.IGNORECASE)
        
        data.append({
            'score': score,
            'GEN': gen_match.group(1) if gen_match else 'Unknown',
            'NP': int(np_match.group(1)) if np_match else 0,
            'TAB': int(tab_match.group(1)) if tab_match else 0,
            'UPF': upf_match.group(1) if upf_match else '0',
            'exp': exp
        })
    
    return pd.DataFrame(data)

def _create_boxplot_figure(df, parameters, np_values, title="Experiment Performance by Parameter"):
    """Shared logic to create boxplot figure with parameter grouping"""
    fig = go.Figure()
    
    # Create traces for each combination of parameter and NP
    for param in parameters:
        for np_val in np_values:
            df_filtered = df[df['NP'] == np_val]
            
            # Get unique values for this parameter
            param_values = sorted(df_filtered[param].unique())
            
            for param_val in param_values:
                df_param = df_filtered[df_filtered[param] == param_val]
                
                fig.add_trace(go.Box(
                    y=df_param['score'],
                    name=f"{param_val}",
                    visible=False,  # Hidden by default
                    # boxmean='sd',
                    boxpoints='all',
                    hovertemplate=f'{param}={param_val}<br>Score: %{{y:.2f}}<extra></extra>'
                ))
    
    # Make first parameter with first NP visible
    traces_per_param_np = {}
    idx = 0
    for param in parameters:
        for np_val in np_values:
            df_filtered = df[df['NP'] == np_val]
            param_values = sorted(df_filtered[param].unique())
            num_traces = len(param_values)
            traces_per_param_np[(param, np_val)] = (idx, idx + num_traces)
            idx += num_traces
    
    # Set first group visible
    first_key = (parameters[0], np_values[0])
    start, end = traces_per_param_np[first_key]
    for i in range(start, end):
        fig.data[i].visible = True
    
    # Create buttons - need to show all combinations
    all_buttons = []
    
    # Create combined buttons for each parameter + NP combination
    for param_idx, param in enumerate(parameters):
        for np_idx, np_val in enumerate(np_values):
            visible_list = [False] * len(fig.data)
            if (param, np_val) in traces_per_param_np:
                start, end = traces_per_param_np[(param, np_val)]
                for k in range(start, end):
                    visible_list[k] = True
            
            button = dict(
                label=f"{param} | NP={np_val}",
                method="update",
                args=[{"visible": visible_list}]
            )
            all_buttons.append(button)
    
    # Set first button as active
    all_buttons[0]["args"][0]["visible"][0:traces_per_param_np[(parameters[0], np_values[0])][1]] = [True] * len(range(*traces_per_param_np[(parameters[0], np_values[0])]))
    
    # Single dropdown with all combinations
    updatemenus = [
        dict(
            buttons=all_buttons,
            direction="down",
            pad={"r": 10, "t": 10},
            showactive=True,
            x=1.0,
            xanchor="left",
            y=1.0,
            yanchor="top",
            bgcolor="lightgray",
            bordercolor="gray",
            font=dict(size=12)
        )
    ]
    
    fig.update_layout(
        title=f"{title}<br><sub>Select parameter and number of pieces</sub>",
        yaxis_title="Final Best Fitness Score",
        xaxis_title="Parameter Value",
        updatemenus=updatemenus,
        height=700,
        width=1200,
        showlegend=False,
        template="plotly_dark",
        paper_bgcolor="#56609b",
        plot_bgcolor="#3d4570",
        font=dict(color="white"),
        annotations=[
            dict(text="Filter:", x=1.0, y=1.02, xref="paper", yref="paper", 
                 showarrow=False, xanchor="left", font=dict(size=14, color="white"))
        ]
    )
    
    return fig

def create_GA_boxplot(exp_list, parameters=['GEN', 'POP', 'MUT']):
    """Create interactive boxplot for GA experiments"""
    df = parse_GA_params(exp_list)
    np_values = sorted(df['NP'].unique())
    return _create_boxplot_figure(df, parameters, np_values, "GA Experiment Performance by Parameter")

def create_SA_boxplot(exp_list, parameters=['GEN', 'TAB', 'UPF']):
    """Create interactive boxplot for SA experiments"""
    df = parse_SA_params(exp_list)
    np_values = sorted(df['NP'].unique())
    return _create_boxplot_figure(df, parameters, np_values, "SA Experiment Performance by Parameter")



# =======================================================================================================
#                                               LINE PLOTS
# =======================================================================================================

def plot_experiments_GA(exp_list):
    fig = go.Figure()

    for exp in exp_list:
        gens, fits = exp.load_log()
        fig.add_trace(go.Scatter(
            x=gens,
            y=fits,
            mode='lines',
            name=repr(exp),
            hovertemplate='Generation: %{x}<br>Best Fitness: %{y:.2f}<extra></extra>'
        ))

    fig.update_layout(
        title="Generation vs Best Fitness - All Experiments",
        xaxis_title="Generation",
        yaxis_title="Best Fitness",
        hovermode='closest',
        width=1200,
        height=700,
        legend=dict(
            yanchor="top",
            y=0.99,
            xanchor="left",
            x=1.01
        ),
        template="plotly_dark",
        paper_bgcolor="#56609b",
        plot_bgcolor="#3d4570",
        font=dict(color="white")
    )

    fig.show()


def plot_experiments_SA(exp_list):
    fig = go.Figure()

    for exp in exp_list:
        gens, fits, temps, gens_best, fits_best = exp.load_log()
        fig.add_trace(go.Scatter(
            x=gens,
            y=fits,
            mode='lines',
            name=repr(exp),
            hovertemplate='Generation: %{x}<br>Best Fitness: %{y:.2f}<extra></extra>'
        ))

    fig.update_layout(
        title="Generation vs Best Fitness - All Experiments",
        xaxis_title="Generation",
        yaxis_title="Best Fitness",
        hovermode='closest',
        width=1200,
        height=700,
        legend=dict(
            yanchor="top",
            y=0.99,
            xanchor="left",
            x=1.01
        ),
        template="plotly_dark",
        paper_bgcolor="#56609b",
        plot_bgcolor="#3d4570",
        font=dict(color="white")
    )

    fig.show()


def plot_experiments_GA_by_pieces(exp_list):
    """Plot GA experiments with lines colored by number of pieces"""
    fig = go.Figure()
    
    # Define colors for each piece count
    colors = {10: '#3498db', 20: '#e74c3c', 30: '#2ecc71'}  # Blue, Red, Green
    
    # Group by pieces for legend organization
    grouped = {}
    for exp in exp_list:
        if exp.pieces not in grouped:
            grouped[exp.pieces] = []
        grouped[exp.pieces].append(exp)
    
    # Plot each group
    for pieces in sorted(grouped.keys()):
        for idx, exp in enumerate(grouped[pieces]):
            gens, fits = exp.load_log()
            fig.add_trace(go.Scatter(
                x=gens,
                y=fits,
                mode='lines',
                name=f"{repr(exp)}",
                legendgroup=f"{pieces} pieces",
                legendgrouptitle_text=f"{pieces} Pieces" if idx == 0 else None,
                line=dict(color=colors.get(pieces, '#95a5a6')),
                hovertemplate='Generation: %{x}<br>Best Fitness: %{y:.2f}<extra></extra>'
            ))
    
    fig.update_layout(
        title="GA: Generation vs Best Fitness - Colored by Pieces",
        xaxis_title="Generation",
        yaxis_title="Best Fitness",
        hovermode='closest',
        width=1200,
        height=700,
        template="plotly_dark",
        paper_bgcolor="#56609b",
        plot_bgcolor="#3d4570",
        font=dict(color="white"),
        legend=dict(
            yanchor="top",
            y=0.99,
            xanchor="left",
            x=1.01
        )
    )
    
    fig.show()


def plot_experiments_SA_by_pieces(exp_list):
    """Plot SA experiments with lines colored by number of pieces"""
    fig = go.Figure()
    
    # Define colors for each piece count
    colors = {10: '#3498db', 20: '#e74c3c', 30: '#2ecc71'}  # Blue, Red, Green
    
    # Group by pieces for legend organization
    grouped = {}
    for exp in exp_list:
        if exp.pieces not in grouped:
            grouped[exp.pieces] = []
        grouped[exp.pieces].append(exp)
    
    # Plot each group
    for pieces in sorted(grouped.keys()):
        for idx, exp in enumerate(grouped[pieces]):
            gens, fits, temps, gens_best, fits_best = exp.load_log()
            fig.add_trace(go.Scatter(
                x=gens,
                y=fits,
                mode='lines',
                name=f"{repr(exp)}",
                legendgroup=f"{pieces} pieces",
                legendgrouptitle_text=f"{pieces} Pieces" if idx == 0 else None,
                line=dict(color=colors.get(pieces, '#95a5a6')),
                hovertemplate='Generation: %{x}<br>Best Fitness: %{y:.2f}<extra></extra>'
            ))
    
    fig.update_layout(
        title="SA: Generation vs Best Fitness - Colored by Pieces",
        xaxis_title="Generation",
        yaxis_title="Best Fitness",
        hovermode='closest',
        width=1200,
        height=700,
        template="plotly_dark",
        paper_bgcolor="#56609b",
        plot_bgcolor="#3d4570",
        font=dict(color="white"),
        legend=dict(
            yanchor="top",
            y=0.99,
            xanchor="left",
            x=1.01
        )
    )
    
    fig.show()
