﻿using System;
using Jasper.Codegen;
using Jasper.Codegen.Compilation;
using JasperBus.Runtime;

namespace JasperBus.Model
{
    public class MessageFrame : Frame
    {
        private readonly MessageVariable _message;
        private readonly Variable _envelope;

        public MessageFrame(MessageVariable message, Variable envelope) : base(false)
        {
            _message = message;
            _envelope = envelope;
        }

        public override void GenerateCode(IGenerationModel generationModel, ISourceWriter writer)
        {
            writer.Write($"var {_message.Usage} = ({_message.VariableType.NameInCode()}){_envelope.Usage}.{nameof(Envelope.Message)};");
            Next?.GenerateCode(generationModel, writer);
        }
    }
}