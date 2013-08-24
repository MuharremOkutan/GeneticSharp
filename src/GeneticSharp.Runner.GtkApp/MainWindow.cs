using System;
using System.Collections.Generic;
using System.Threading;
using Gdk;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Reinsertions;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Runner.GtkApp;
using GeneticSharp.Runner.GtkApp.Samples;
using Gtk;

/// <summary>
/// Main window.
/// </summary>
public partial class MainWindow: Gtk.Window
{	
	#region Fields
	private GeneticAlgorithm m_ga;
	private IFitness m_fitness;
	private ISelection m_selection;
	private ICrossover m_crossover;
	private IMutation m_mutation;
    private IReinsertion m_reinsertion;
	private ITermination m_termination;
    private IGenerationStrategy m_generationStrategy;
    
	private ISampleController m_sampleController;
    private SampleContext m_sampleContext;
	private Thread m_evolvingThread;
    #endregion

	#region Constructors
	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();

		DeleteEvent+=delegate {Application.Quit(); };
	   
		btnEvolve.Clicked += delegate 
        {
			if(vbxButtonBar.Sensitive)
			{
				m_evolvingThread = new Thread(Run);                        
				m_evolvingThread.Start();
			}
			else {
				btnEvolve.Label = "Stopping...";
				btnEvolve.Sensitive = false;
				m_evolvingThread.Abort();
			}
        };
	
		drawingArea.ConfigureEvent += delegate {
			ResetBuffer ();
            UpdateSample();
		};

		drawingArea.ExposeEvent += delegate {
			DrawBuffer();
		};       		

	    ShowAll();

        PrepareComboBoxes();
        PrepareSamples();

        cmbSample.Active = 1;
        cmbCrossover.Active = 2;
        cmbTermination.Active = 2;
        hslCrossoverProbability.Value = GeneticAlgorithm.DefaultCrossoverProbability;
        hslMutationProbability.Value = GeneticAlgorithm.DefaultMutationProbability;

		ResetBuffer ();
        ResetSample();
	}    
	#endregion

	#region GA methods
    private void Run()
    {
        try
        {
			Application.Invoke(delegate
			{
        		vbxButtonBar.Sensitive = false;
				btnEvolve.Label = "_Stop";
			});

            if (m_ga != null)
            {
                m_ga.GenerationRan -= HandleGAUpdated;
                m_ga.TerminationReached -= HandleGAUpdated;
            }

            m_sampleController.Reset();
            m_sampleContext.Population = new Population(
                Convert.ToInt32(sbtPopulationMinSize.Value),
                Convert.ToInt32(sbtPopulationMaxSize.Value),
                m_sampleController.CreateChromosome());

            m_sampleContext.Population.GenerationStrategy = m_generationStrategy;

            m_ga = new GeneticAlgorithm(
                m_sampleContext.Population,
                m_fitness,
                m_selection,
                m_crossover,
                m_mutation);

            m_ga.CrossoverProbability = Convert.ToSingle(hslCrossoverProbability.Value);
            m_ga.MutationProbability = Convert.ToSingle(hslMutationProbability.Value);
            m_ga.GenerationRan += HandleGAUpdated;
            m_ga.TerminationReached -= HandleGAUpdated;

            m_ga.Reinsertion = m_reinsertion;
            m_ga.Termination = m_termination;

            m_ga.Start();
	    }
		catch(ThreadAbortException) {
			Console.WriteLine ("Thread aborted.");
		}
        catch (Exception ex)
        {
			Application.Invoke(delegate
			{
				var msg = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.YesNo, "{0}\n\nDo you want to see more details about this error?", ex.Message);

	            if (msg.Run() == (int)ResponseType.Yes)
	            {
	                var details = new MessageDialog(this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, ex.StackTrace);
	                details.Run();
	                details.Destroy();
	            }

	            msg.Destroy();
			});
        }
        finally
        {           
			Application.Invoke(delegate
			{
				vbxButtonBar.Sensitive = true;
				btnEvolve.Label = "_Start";
				btnEvolve.Sensitive = true;
			});
        }
    }
    #endregion

    #region Sample methods
    private void PrepareSamples()
    {
        LoadComboBox(cmbSample, SampleService.GetSampleControllerNames());
        m_sampleController = SampleService.CreateSampleControllerByName(cmbSample.ActiveText);

        // Sample context.
        var layout = new Pango.Layout(this.PangoContext);
        layout.Alignment = Pango.Alignment.Center;
        layout.FontDescription = Pango.FontDescription.FromString("Arial 16");

        m_sampleContext = new SampleContext(drawingArea.GdkWindow, this)
        {
            Layout = layout
        };

        m_sampleContext.GC = m_sampleContext.CreateGC(new Gdk.Color(255, 50, 50));

        m_sampleController.Context = m_sampleContext;
        m_sampleController.Reconfigured += delegate
        {
            ResetSample();
        };

        problemConfigWidgetContainer.Add(m_sampleController.CreateConfigWidget());

        cmbSample.Changed += delegate
        {
            m_sampleController = SampleService.CreateSampleControllerByName(cmbSample.ActiveText);
            m_sampleController.Context = m_sampleContext;
            m_sampleController.Reconfigured += delegate
            {
                ResetSample();
            };

            if (problemConfigWidgetContainer.Children.Length > 0)
            {
                problemConfigWidgetContainer.Children[0].Destroy();
            }

            problemConfigWidgetContainer.Add(m_sampleController.CreateConfigWidget());
            problemConfigWidgetContainer.ShowAll();

            ResetBuffer();
            ResetSample();
        };
    }

    private void HandleGAUpdated(object sender, EventArgs e)
    {
        UpdateSample();
    }

    private void UpdateSample()
    {
        if (m_sampleContext.Population == null)
        {
            Application.Invoke(delegate
            {
                DrawSample();
            });
        }
        else
        {
            // Avoid to update the map so quickly that makes the UI freeze.
            if (m_sampleContext.Population.GenerationsNumber % 10 == 0)
            {
                Application.Invoke(delegate
                {
                    m_sampleController.Update();
                    DrawSample();
                });
            }
        }
    }

    private void ResetSample()
    {
        m_sampleContext.Population = null;
        var r = drawingArea.Allocation;
        m_sampleContext.DrawingArea = new Rectangle(0, 100, r.Width, r.Height - 100);
        m_sampleController.Reset();
        m_fitness = m_sampleController.CreateFitness();
        UpdateSample();
    }

    private void DrawSample()
    {
        m_sampleContext.Reset();
        m_sampleContext.Buffer.DrawRectangle(drawingArea.Style.WhiteGC, true, 0, 0, drawingArea.Allocation.Width, drawingArea.Allocation.Height);

        m_sampleController.Draw();

        if (m_sampleContext.Population != null)
        {
            m_sampleContext.WriteText("Generation: {0}", m_sampleContext.Population.GenerationsNumber);
            m_sampleContext.WriteText("Fitness: {0:n2}", m_sampleContext.Population.BestChromosome.Fitness);
            m_sampleContext.WriteText("Time: {0}", m_ga.TimeEvolving);
        }

        DrawBuffer();
    }	
    #endregion

    #region Form methods
    private void PrepareComboBoxes()
    {
        PrepareEditComboBox(
           cmbSelection,
           btnEditSelection,
           SelectionService.GetSelectionNames,
           SelectionService.GetSelectionTypeByName,
           SelectionService.CreateSelectionByName,
           () => m_selection,
           (i) => m_selection = i);

        PrepareEditComboBox(
            cmbCrossover,
            btnEditCrossover,
            CrossoverService.GetCrossoverNames,
            CrossoverService.GetCrossoverTypeByName,
            CrossoverService.CreateCrossoverByName,
            () => m_crossover,
            (i) => m_crossover = i);

        PrepareEditComboBox(
            cmbMutation,
            btnEditMutation,
            MutationService.GetMutationNames,
            MutationService.GetMutationTypeByName,
            MutationService.CreateMutationByName,
            () => m_mutation,
            (i) => m_mutation = i);

        PrepareEditComboBox(
            cmbTermination,
            btnEditTermination,
            TerminationService.GetTerminationNames,
            TerminationService.GetTerminationTypeByName,
            TerminationService.CreateTerminationByName,
            () => m_termination,
            (i) => m_termination = i);

        PrepareEditComboBox(
            cmbTermination1,
            btnEditReinsertion,
            ReinsertionService.GetReinsertionNames,
            ReinsertionService.GetReinsertionTypeByName,
            ReinsertionService.CreateReinsertionByName,
            () => m_reinsertion,
            (i) => m_reinsertion = i);

        PrepareEditComboBox(
            cmbGenerationStrategy,
            btnEditGenerationStrategy,
            PopulationService.GetGenerationStrategyNames,
            PopulationService.GetGenerationStrategyTypeByName,
            PopulationService.CreateGenerationStrategyByName,
            () => m_generationStrategy,
            (i) => m_generationStrategy = i);
    }

    private void PrepareEditComboBox<TItem>(ComboBox comboBox, Button editButton, Func<IList<string>> getNames, Func<string, Type> getTypeByName, Func<string, object[], TItem> createItem, Func<TItem> getItem, Action<TItem> setItem)
    {
        // ComboBox.
        LoadComboBox(comboBox, getNames());

        comboBox.Changed += delegate
        {
            var item = createItem(comboBox.ActiveText, new object[0]);
            setItem(item);
            ShowButtonByEditableProperties(editButton, item);
        };

        setItem(createItem(comboBox.ActiveText, new object[0]));

        comboBox.ExposeEvent += delegate
        {
            ShowButtonByEditableProperties(editButton, getItem());
        };

        // Edit button.
        editButton.Clicked += delegate
        {
            var editor = new PropertyEditor(getTypeByName(comboBox.ActiveText), getItem());
            editor.Run();
            setItem((TItem)editor.ObjectInstance);
        };
    }

    private void LoadComboBox(ComboBox cmb, IList<string> names)
    {
        foreach (var c in names)
        {
            cmb.AppendText(c);
        }

        cmb.Active = 0;
    }

    private void ShowButtonByEditableProperties(Button editButton, object item)
    {
        if (PropertyEditor.HasEditableProperties(item.GetType()))
        {
            editButton.Show();
        }
        else
        {
            editButton.Hide();
        }
    }
   
	private void ResetBuffer()
	{
        m_sampleContext.Buffer = new Pixmap(drawingArea.GdkWindow, drawingArea.Allocation.Width, drawingArea.Allocation.Height);
	}
	
	private void DrawBuffer()
	{
        drawingArea.GdkWindow.DrawDrawable(m_sampleContext.GC, m_sampleContext.Buffer, 0, 0, 0, 0, drawingArea.Allocation.Width, drawingArea.Allocation.Height);
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}	
	#endregion
}
