// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import { JSONSchema7 } from '@bfc/plugin-loader';
const schema: JSONSchema7 = {
  type: 'object',
  properties: {
    subscriptionID: {
      type: 'string',
      title: 'Subscription ID',
      examples: ['<your subscription id>'],
    },
    publishName: {
      type: 'string',
      title: 'publishName',
    },
    environment: {
      type: 'string',
      title: 'Environment',
    },
    location: {
      type: 'string',
      title: 'Location',
    },
    luisAuthoringRegion: {
      type: 'string',
      title: 'region',
    },
    luisAuthoringKey: {
      type: 'string',
      title: 'Authoring Key',
    },
    provision: {
      type: 'object',
      title: 'Provision resource',
      properties: {
        applicationInsights: {
          type: 'object',
          properties: {
            InstrumentationKey: {
              type: 'string',
            },
          },
        },
        cosmosDb: {
          type: 'object',
          properties: {
            cosmosDBEndpoint: {
              type: 'string',
            },
            authKey: {
              type: 'string',
            },
            databaseId: {
              type: 'string',
            },
            collectionId: {
              type: 'string',
            },
            containerId: {
              type: 'string',
            },
          },
          required: ['cosmosDBEndpoint', 'authKey', 'databaseId', 'collectionId', 'containerId'],
        },
        blobStorage: {
          type: 'object',
          properties: {
            connectionString: {
              type: 'string',
            },
            container: {
              type: 'string',
            },
          },
          required: ['connectionString', 'container'],
        },
        luis: {
          type: 'object',
          properties: {
            endpointKey: {
              type: 'string',
            },
            authoringKey: {
              type: 'string',
            },
            region: {
              type: 'string',
            },
          },
          required: ['endpointKey', 'authoringKey', 'region'],
        },
        MicrosoftAppId: {
          type: 'string',
        },
        MicrosoftAppPassword: {
          type: 'string',
        },
      },
      required: ['MicrosoftAppId', 'MicrosoftAppPassword'],
    },
    accessToken: {
      type: 'string',
    },
  },
  required: ['subscriptionID', 'publishName', 'provision', 'accessToken'],
  default: {
    subscriptionID: '<your subscription id>',
    publishName: '<unique name in your subscription>',
    environment: 'dev',
    location: 'westus',
    luisAuthoringRegion: 'westus',
    luisAuthoringKey: '',
    provision: {},
  },
};
export default schema;
