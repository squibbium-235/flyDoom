using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;
using FlyDoom.Neural.Models;
using FlyDoom.Neural.Simulation;
using FlyDoom.Neural.Transmission;

namespace FlyDoom.Tests;

public sealed class NeuralSimulationTests
{
    [Fact]
    public void Step_AdvancesSimulationTime()
    {
        var simulation =
            CreateThreeNeuronChain();

        simulation.Step(1f);

        Assert.Equal(
            1d,
            simulation.SimulationTimeMs);

        simulation.Step(1f);

        Assert.Equal(
            2d,
            simulation.SimulationTimeMs);
    }

    [Fact]
    public void Spike_PropagatesToConnectedNeuron()
    {
        var simulation =
            CreateThreeNeuronChain();

        // Neuron 0 receives external stimulation strong enough
        // to make it fire during the first timestep.
        simulation.State.AddSynapticDriveMv(
            0,
            20f);

        simulation.Step(1f);

        Assert.True(
            simulation.State.DidFire(0));

        Assert.False(
            simulation.State.DidFire(1));

        // Neuron 0's spike has now queued input for neuron 1.
        Assert.Equal(
            20f,
            simulation.State.GetSynapticDriveMv(1));

        simulation.Step(1f);

        Assert.True(
            simulation.State.DidFire(1));
    }

    [Fact]
    public void Spike_CanPropagateAlongNeuronChain()
    {
        var simulation =
            CreateThreeNeuronChain();

        simulation.State.AddSynapticDriveMv(
            0,
            20f);

        simulation.Step(1f);

        Assert.True(
            simulation.State.DidFire(0));

        simulation.Step(1f);

        Assert.True(
            simulation.State.DidFire(1));

        simulation.Step(1f);

        Assert.True(
            simulation.State.DidFire(2));
    }

    [Fact]
    public void SynapseCount_ScalesSyntheticTransmission()
    {
        var connectome =
            new CompactConnectome(
                outgoingOffsets:
                [
                    0,
                    1,
                    1
                ],
                postsynapticIndices:
                [
                    1
                ],
                synapseCounts:
                [
                    3
                ],
                neuropilIndices:
                [
                    0
                ],
                neurotransmitterTypes:
                [
                    NeurotransmitterType.Unknown
                ],
                neuropilNames:
                [
                    "TEST"
                ]);

        var parameters =
            CreateTestParameters();

        var state =
            new NeuronStateTable(
                connectome.NeuronCount,
                parameters.RestingPotentialMv);

        var simulation =
            new NeuralSimulation(
                connectome,
                state,
                new LifNeuronModel(parameters),
                new FixedSynapticEffectModel(
                    drivePerSynapseMv: 2f));

        state.AddSynapticDriveMv(
            0,
            20f);

        simulation.Step(1f);

        Assert.Equal(
            6f,
            state.GetSynapticDriveMv(1));
    }

    [Fact]
    public void Constructor_RejectsMismatchedNeuronCounts()
    {
        var connectome =
            CreateChainConnectome();

        var parameters =
            CreateTestParameters();

        var state =
            new NeuronStateTable(
                2,
                parameters.RestingPotentialMv);

        Assert.Throws<ArgumentException>(
            () =>
                new NeuralSimulation(
                    connectome,
                    state,
                    new LifNeuronModel(parameters),
                    new FixedSynapticEffectModel(20f)));
    }

    private static NeuralSimulation
        CreateThreeNeuronChain()
    {
        var connectome =
            CreateChainConnectome();

        var parameters =
            CreateTestParameters();

        var state =
            new NeuronStateTable(
                connectome.NeuronCount,
                parameters.RestingPotentialMv);

        var neuronModel =
            new LifNeuronModel(
                parameters);

        var synapticEffectModel =
            new FixedSynapticEffectModel(
                drivePerSynapseMv: 20f);

        return new NeuralSimulation(
            connectome,
            state,
            neuronModel,
            synapticEffectModel);
    }

    private static CompactConnectome
        CreateChainConnectome()
    {
        // Three neurons:
        //
        // 0 -> 1 -> 2
        //
        // Neuron 2 has no outgoing connections.
        return new CompactConnectome(
            outgoingOffsets:
            [
                0,
                1,
                2,
                2
            ],
            postsynapticIndices:
            [
                1,
                2
            ],
            synapseCounts:
            [
                1,
                1
            ],
            neuropilIndices:
            [
                0,
                0
            ],
            neurotransmitterTypes:
            [
                NeurotransmitterType.Unknown,
                NeurotransmitterType.Unknown
            ],
            neuropilNames:
            [
                "TEST"
            ]);
    }

    private static LifNeuronParameters
        CreateTestParameters()
    {
        return new LifNeuronParameters(
            restingPotentialMv: -60f,
            resetPotentialMv: -65f,
            thresholdPotentialMv: -50f,
            membraneTimeConstantMs: 1f,
            refractoryPeriodMs: 2f);
    }
}