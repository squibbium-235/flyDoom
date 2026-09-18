using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;
using FlyDoom.Neural.Models;
using FlyDoom.Neural.Simulation;
using FlyDoom.Neural.Transmission;

namespace FlyDoom.Tests;

public sealed class NeuralResetTests
{
    [Fact]
    public void NeuronStateReset_ClearsTransientState()
    {
        var parameters =
            CreateTestParameters();

        var state =
            new NeuronStateTable(
                neuronCount: 2,
                initialMembranePotentialMv:
                    parameters.RestingPotentialMv);

        var model =
            new LifNeuronModel(
                parameters);

        state.AddSynapticInputMv(
            0,
            12f);

        state.AddExternalDriveMv(
            1,
            20f);

        model.Step(
            state,
            1f);

        Assert.True(
            state.DidFire(
                1));

        state.Reset();

        for (var neuronIndex = 0;
             neuronIndex < state.Count;
             neuronIndex++)
        {
            Assert.Equal(
                parameters.RestingPotentialMv,
                state.GetMembranePotentialMv(
                    neuronIndex));

            Assert.Equal(
                0f,
                state.GetSynapticInputMv(
                    neuronIndex));

            Assert.Equal(
                0f,
                state.GetExternalDriveMv(
                    neuronIndex));

            Assert.Equal(
                0f,
                state.GetRefractoryRemainingMs(
                    neuronIndex));

            Assert.False(
                state.DidFire(
                    neuronIndex));
        }
    }

    [Fact]
    public void NeuralSimulationReset_RestoresStateAndTime()
    {
        var parameters =
            CreateTestParameters();

        var connectome =
            CreateConnectome();

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
                    inputPerSynapseMv:
                        20f));

        state.AddExternalDriveMv(
            0,
            20f);

        simulation.Step(
            1f);

        Assert.Equal(
            1d,
            simulation.SimulationTimeMs);

        Assert.True(
            state.DidFire(
                0));

        Assert.True(
            state.GetSynapticInputMv(
                1) >
            0);

        simulation.Reset();

        Assert.Equal(
            0d,
            simulation.SimulationTimeMs);

        for (var neuronIndex = 0;
             neuronIndex < state.Count;
             neuronIndex++)
        {
            Assert.Equal(
                parameters.RestingPotentialMv,
                state.GetMembranePotentialMv(
                    neuronIndex));

            Assert.Equal(
                0f,
                state.GetSynapticInputMv(
                    neuronIndex));

            Assert.False(
                state.DidFire(
                    neuronIndex));
        }
    }

    private static CompactConnectome CreateConnectome()
    {
        return new CompactConnectome(
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
                1
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