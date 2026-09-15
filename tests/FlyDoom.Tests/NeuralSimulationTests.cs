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
    public void Spike_PropagatesSynapticInput()
    {
        var simulation =
            CreateThreeNeuronChain();

        simulation.State.AddExternalDriveMv(
            0,
            20f);

        simulation.Step(1f);

        Assert.True(
            simulation.State.DidFire(0));

        Assert.False(
            simulation.State.DidFire(1));

        Assert.Equal(
            20f,
            simulation.State.GetSynapticInputMv(1));
    }

    [Fact]
    public void Spike_CanPropagateAlongNeuronChain()
    {
        var simulation =
            CreateThreeNeuronChain();

        simulation.State.AddExternalDriveMv(
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
    public void Step_ReportsActiveSynapticState()
    {
        var simulation =
            CreateThreeNeuronChain();

        simulation.State.AddExternalDriveMv(
            0,
            20f);

        var result =
            simulation.Step(1f);

        Assert.True(
            result.FiredNeuronCount > 0);

        Assert.True(
            result.ActiveSynapticNeuronCount > 0);

        Assert.True(
            result.MaximumAbsoluteSynapticInputMv > 0);
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
                new LifNeuronModel(
                    parameters),
                new FixedSynapticEffectModel(
                    inputPerSynapseMv: 2f));

        state.AddExternalDriveMv(
            0,
            20f);

        simulation.Step(1f);

        Assert.Equal(
            6f,
            state.GetSynapticInputMv(1));
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
                    new LifNeuronModel(
                        parameters),
                    new FixedSynapticEffectModel(
                        20f)));
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

        return new NeuralSimulation(
            connectome,
            state,
            new LifNeuronModel(
                parameters),
            new FixedSynapticEffectModel(
                inputPerSynapseMv: 20f));
    }

    private static CompactConnectome
        CreateChainConnectome()
    {
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
            synapticTimeConstantMs: 5f,
            refractoryPeriodMs: 2f);
    }
}