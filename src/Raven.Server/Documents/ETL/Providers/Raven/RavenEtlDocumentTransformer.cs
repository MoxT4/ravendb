﻿using System.Collections.Generic;
using Raven.Client.Documents.Commands.Batches;
using Raven.Server.Documents.Patch;
using Raven.Server.ServerWide.Context;

namespace Raven.Server.Documents.ETL.Providers.Raven
{
    public class RavenEtlDocumentTransformer : EtlTransformer<RavenEtlItem, ICommandData>
    {
        private readonly List<ICommandData> _commands = new List<ICommandData>();
        private readonly PatchRequest _transformationScript = null;

        public RavenEtlDocumentTransformer(DocumentDatabase database, DocumentsOperationContext context, RavenEtlConfiguration configuration) : base(database, context)
        {
            if (string.IsNullOrEmpty(configuration.Script) == false)
                _transformationScript = new PatchRequest { Script = configuration.Script };
        }

        public override IEnumerable<ICommandData> GetTransformedResults()
        {
            return _commands;
        }

        public override void Transform(RavenEtlItem item)
        {
            if (item.IsDelete)
                _commands.Add(new DeleteCommandData(item.DocumentKey, null));
            else
            {
                if (_transformationScript != null)
                {
                    var result = Apply(Context, item.Document, _transformationScript);

                    _commands.Add(new PutCommandDataWithBlittableJson(item.DocumentKey, null, result.ModifiedDocument));
                }
                else
                    _commands.Add(new PutCommandDataWithBlittableJson(item.DocumentKey, null, item.Document.Data));
            }
        }
    }
}