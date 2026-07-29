import { z } from 'zod'

const optionalStringSchema = z.string().nullish().transform((value) => value ?? undefined)

const httpUrlSchema = z.url().refine((value) => {
  const protocol = new URL(value).protocol
  return protocol === 'http:' || protocol === 'https:'
}, 'Only HTTP and HTTPS URLs are supported.')

export const identifierSchema = z.string().min(1).max(200)

const locatorSchema = z.object({
  strategy: z.enum(['testId', 'id', 'name', 'css', 'xpath']),
  value: z.string().min(1),
  displayName: optionalStringSchema,
})

const pageElementSchema = z.object({
  id: z.string().min(1),
  name: z.string().min(1),
  tag: z.string().min(1),
  action: z.enum([
    'navigate',
    'click',
    'fill',
    'select',
    'pressKey',
    'assertVisible',
    'assertText',
    'assertUrl',
  ]),
  locator: locatorSchema,
  expectedText: optionalStringSchema,
})

export const pageSchema = z.object({
  schemaVersion: z.literal(1),
  id: z.string().min(1),
  name: z.string().min(1),
  url: httpUrlSchema,
  elements: z.array(pageElementSchema),
  updatedAt: z.string(),
})

const stepValueSchema = z.object({
  kind: z.enum(['literal', 'secret']),
  value: z.string(),
})

const testStepSchema = z.object({
  id: z.string().min(1),
  action: z.enum([
    'navigate',
    'click',
    'fill',
    'select',
    'pressKey',
    'assertVisible',
    'assertText',
    'assertUrl',
  ]),
  label: z.string().min(1),
  enabled: z.boolean(),
  pageId: optionalStringSchema,
  elementId: optionalStringSchema,
  url: httpUrlSchema.nullish().transform((value) => value ?? undefined),
  value: stepValueSchema.nullish().transform((value) => value ?? undefined),
  timeoutMs: z.number().int().positive().nullish().transform((value) => value ?? undefined),
})

export const testCaseSchema = z.object({
  schemaVersion: z.literal(1),
  id: z.string().min(1),
  name: z.string().min(1),
  description: optionalStringSchema,
  tags: z.array(z.string()),
  steps: z.array(testStepSchema),
  updatedAt: z.string(),
})

export const scanRequestSchema = z.object({
  url: httpUrlSchema,
  name: z.string().min(1),
  browser: z.enum(['chrome', 'edge']).optional(),
})

export const generationRequestSchema = z.object({
  pageIds: z.array(identifierSchema).optional(),
  testIds: z.array(identifierSchema).optional(),
})

export const startRunRequestSchema = z.object({
  testId: identifierSchema,
  browser: z.enum(['chrome', 'edge']).optional(),
})

export const secretValuesSchema = z.record(identifierSchema, z.string().max(10_000))

export const httpNavigationSchema = httpUrlSchema

export const booleanSchema = z.boolean()

export const boundsSchema = z.object({
  x: z.number().int(),
  y: z.number().int(),
  width: z.number().int().nonnegative(),
  height: z.number().int().nonnegative(),
})

const regexPatternSchema = z.string().min(1).refine((value) => {
  try {
    new RegExp(value)
    return true
  } catch {
    return false
  }
}, 'Must be a valid regular expression.')

export const cliqSettingsSchema = z.object({
  schemaVersion: z.literal(1),
  elementIdentification: z.object({
    testIdAttribute: z.string().min(1),
    strategyOrder: z.array(z.enum(['testId', 'id', 'name'])).min(1),
    ignoreIdPatterns: z.array(regexPatternSchema),
  }),
  testExecution: z.object({
    defaultStepTimeoutMs: z.number().int().positive(),
    pageReadyTimeoutMs: z.number().int().positive(),
    captureScreenshotOnFailure: z.boolean(),
  }),
  scanning: z.object({
    loadTimeoutMs: z.number().int().positive(),
    networkIdleGraceMs: z.number().int().positive(),
    captureScreenshot: z.boolean(),
  }),
})

export const screenshotRequestSchema = z.object({
  runId: identifierSchema,
  fileName: z.string().min(1).max(300),
})

export const recordedInteractionSchema = z.object({
  id: z.string().uuid(),
  action: z.enum(['click', 'fill', 'select']),
  label: z.string().min(1).max(160),
  pageUrl: httpUrlSchema,
  element: pageElementSchema,
  value: stepValueSchema.optional(),
  secretValue: z.string().max(10_000).optional(),
})
