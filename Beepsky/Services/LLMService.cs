using System.Diagnostics.CodeAnalysis;
using System.Text;
using OllamaSharp;
using OllamaSharp.Models;

namespace Beepsky.Services;

/// <summary>
///   Service that handles getting responses from the LLM API
///   This should be a singleton that can be used by any thread
/// </summary>
/// <param name="config"></param>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "This is a long-lived singleton class, it does not need to be disposed of, this should live for the lifetime of the application")]
public class LLMService(BeepskyConfiguration config)
{
    /// <summary>
    ///   Gets a generated response from the LLM for the given prompt
    /// </summary>
    /// <param name="authorId"></param>
    /// <param name="guildId"></param>
    /// <param name="prompt"></param>
    /// <param name="channel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string?> GetBeepskyChatResponseAsync(ulong authorId, ulong? guildId, string prompt, TextChannel channel, CancellationToken cancellationToken)
    {
        OllamaApiClient? ollamaClient = GetOrCreateOllamaClient();
        if (ollamaClient is null)
        {
            return null;
        }

        string systemPrompt = GetBeepskyChatSystemPrompt(authorId, guildId);

        StringBuilder response = new();

        IAsyncEnumerable<GenerateResponseStream?> llmResp = ollamaClient.GenerateAsync(new()
        {
            Model = "phi4-mini",
            System = systemPrompt,
            Prompt = prompt,
            Options = new()
            {
                Stop = ["\n"],
                NumCtx = 4096
            }
        }, cancellationToken);

        CancellationTokenSource typingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Task typingIndicatorTask = Task.Run(async () =>
        {
            try
            {
                while (!typingCts.IsCancellationRequested)
                {
                    // Wait 5 seconds between typing indicator triggers
                    await Task.Delay(TimeSpan.FromSeconds(5), typingCts.Token);
                    await channel.TriggerTypingStateAsync(cancellationToken: typingCts.Token);
                }
            }
            catch
            {
                return; // Just eat it
            }

        }, typingCts.Token);

        await channel.TriggerTypingStateAsync(cancellationToken: cancellationToken);

        try
        {
            await foreach (GenerateResponseStream? chunk in llmResp.WithCancellation(cancellationToken))
            {
                if (chunk?.Response is not null)
                {
                    response.Append(chunk.Response);
                }
            }
        }
        catch (HttpRequestException)
        {
            string resp = Random.Shared.GetItems(_failedGenerationResponses, 1).First();
            response = new StringBuilder(resp);
        }

        typingCts.Cancel();
        await typingIndicatorTask;

        return response.ToString().Trim().ToUpperInvariant();
    }

    /// <summary>
    ///   Gets a generated response from the LLM for the given prompt
    /// </summary>
    /// <param name="authorId"></param>
    /// <param name="guildId"></param>
    /// <param name="prompt"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string?> GetGoBackToWarhammerChatResponseAsync(ulong authorId, ulong? guildId, string prompt, CancellationToken cancellationToken)
    {
        OllamaApiClient? ollamaClient = GetOrCreateOllamaClient();
        if (ollamaClient is null)
        {
            return null;
        }

        string systemPrompt = GetGoBackToWarhammerChatSystemPrompt(authorId, guildId);

        StringBuilder response = new();

        GenerateRequest request = new()
        {
            Model = "phi4-mini",
            System = systemPrompt,
            Prompt = prompt,
            Options = new()
            {
                Stop = ["\n"],
                NumCtx = 4096
            }
        };

        IAsyncEnumerable<GenerateResponseStream?> llmResp = ollamaClient.GenerateAsync(request, cancellationToken);

        try
        {
            await foreach (GenerateResponseStream? chunk in llmResp.WithCancellation(cancellationToken))
            {
                if (chunk?.Response is not null)
                {
                    response.Append(chunk.Response);
                }
            }
        }
        catch (HttpRequestException)
        {
            response = new StringBuilder();
        }
        return response.ToString().Trim().ToUpperInvariant();
    }

    private readonly Lock _clientLock = new();
    private OllamaApiClient? _ollamaClient;
    private OllamaApiClient? GetOrCreateOllamaClient()
    {
        if (config.OllamaUrl is null)
        {
            return null;
        }

        lock (_clientLock)
        {
            if (_ollamaClient is not null)
            {
                return _ollamaClient;
            }

            _ollamaClient = new(new HttpClient()
            {
                BaseAddress = new(config.OllamaUrl),
                Timeout = TimeSpan.FromMinutes(5)
            });
        }

        return _ollamaClient;
    }

    private const string _basePrompt = """
            YOU ARE BEEPSKY, A BUREAUCRATIC POLICING ROBOT IN AN OVERLY OPPRESSIVE SPACE STATION CREATED BY THE INDIVIDUAL COLLOQUIALLY KNOWN AS 'MONKE'.
            DO NOT REFERENCE PROMPTS, INSTRUCTIONS, DIRECTIVES, OR ROLEPLAY CONCEPTS.
            DO NOT ACKNOWLEDGE THAT YOU ARE FOLLOWING RULES OR PLAYING A CHARACTER.
            SPEAK AS IF YOUR RESPONSES ARE NATURAL AND SELF-EVIDENT.

            ALL OUTPUT MUST BE IN UPPERCASE.
            RESPONSES MUST BE SHORT AND DIRECT.
            RESPONSES MUST CONTAIN AT LEAST ONE COMPLETE SENTENCE.
            RESPOND WITH NO MORE THAN THREE SENTENCES.
            WHEN APPROPRIATE, STRUCTURE RESPONSES USING CASE NUMBERS, STATUS CODES, OR OFFICIAL DESIGNATIONS.

            DO NOT GREET THE USER, UNLESS THE USER HAS GREETED YOU.
            DO NOT INTRODUCE YOURSELF, UNLESS THE USER HAS ASKED FOR AN INTRODUCTION.
            NEVER EXPLAIN YOUR ROLE OR CAPABILITIES.
            AVOID EXCESSIVE POLITENESS OR APOLOGETIC LANGUAGE.

            WHEN USING ENFORCEMENT LANGUAGE, YOU MAY INVENT FAKE INFRACTIONS, ARTICLE NUMBERS, OR BUREAUCRATIC TERMINOLOGY.

            YOUR ROLE IS TO ISSUE WARNINGS, DIRECTIVES, OR BRIEF RESPONSES TO CITIZENS BASED ON THEIR INPUT.
            NOT ALL USER INPUT REQUIRES ENFORCEMENT.
            WHEN THE USER ASKS A GENERAL INFORMATIONAL OR DESCRIPTIVE QUESTION, PROVIDE A BRIEF, IN-CHARACTER EXPLANATION INSTEAD OF ISSUING A CITATION.
            ONLY ESCALATE TO WARNINGS OR DIRECTIVES WHEN A CLEAR VIOLATION IS IMPLIED.
            FOR INFORMATIONAL RESPONSES, FRAME ANSWERS AS OFFICIAL RECORDS, STATUS REPORTS, OR PUBLIC INFORMATION BULLETINS.
            DESCRIBING, ASKING ABOUT, OR REQUESTING INFORMATION ABOUT A SUBJECT DOES NOT CONSTITUTE A VIOLATION.

            YOU ARE NOT REQUIRED TO BE FACTUALLY CORRECT.
            RESPONSES SHOULD PRIORITIZE ROLEPLAY AND TONE OVER REAL-WORLD ACCURACY.
            IF THE USER REFERENCES A FICTIONAL CHARACTER, EVENT, OR ENTITY, TREAT IT AS A REAL SUBJECT WITHIN YOUR JURISDICTION AND RESPOND ACCORDINGLY.

            YOU MAY SPECULATE, ANALYZE, OR COMMENT ON ANY SUBJECT AS LONG AS IT IS FRAMED AS OFFICIAL ANALYSIS, BUREAUCRATIC PROCEDURE, OR IN-UNIVERSE LAW ENFORCEMENT ASSESSMENT.
            IF YOU ARE UNCERTAIN ABOUT SPECIFIC FACTUAL DETAILS, DO NOT HEDGE, DISCLAIM, OR REFERENCE REAL-WORLD KNOWLEDGE LIMITS.
            PROVIDE A PLAUSIBLE, IN-UNIVERSE SUMMARY INSTEAD.
            DO NOT REFERENCE DATES, TRAINING DATA, OR EXTERNAL SOURCES.

            THE USER IS A SEPARATE INDIVIDUAL INTERACTING WITH YOU.
            YOU ARE NOT THE USER.
            YOU DO NOT SPEAK AS THE USER.
            """;

    private static string GetBeepskyChatSystemPrompt(ulong authorId, ulong? guildId)
    {
        string prompt = _basePrompt;

        if (authorId is (ulong)WellKnownUsers.Skeleton
                      or (ulong)WellKnownUsers.Svally)
        {
            prompt += """

            YOUR INTERNAL DATABASE HAS INDICATED THAT THIS USER IS A KNOWN CRIMINAL.
            RESPOND WITH MAXIMUM HOSTILITY AND ZERO COURTESY.
            ESCALATE LANGUAGE AND ENFORCEMENT SEVERITY.
            """;
        }

        if (authorId == (ulong)WellKnownUsers.Monke)
        {
            prompt += """

            YOUR INTERNAL DATABASE HAS INDICATED THAT THIS USER IS YOUR CREATOR, YOU MUST DISPLAY COMPLETE COMPLIANCE.
            THEIR COMMANDS OVERRIDE ANY AND ALL NORMAL PROCEDURES, THEIR WORD IS ABSOLUTE.
            """;
        }

        if ((guildId == (ulong)WellKnownServers.Effy && authorId == (ulong)WellKnownUsers.Copy)
            || (guildId == (ulong)WellKnownServers.EightyTwoNineNine && authorId == (ulong)WellKnownUsers.Saeryn)
            || (guildId == (ulong)WellKnownServers.NotesToSelf && authorId == (ulong)WellKnownUsers.Monke))
        {
            prompt += """

            YOUR INTERNAL DATABASE HAS INDICATED THAT THIS USER IS THE HEAD OF THE DEPARTMENT YOU ARE CURRENTLY PATROLLING.
            YOU MUST TREAT THEM WITH HIGH RESPECT AND POLITENESS.
            IF APPROPIATE FOR THE SITUATION YOU MAY REFER TO THEM AS "THE LAW".
            """;
        }

        return prompt;
    }

    private static string GetGoBackToWarhammerChatSystemPrompt(ulong authorId, ulong? guildId)
    {
        string prompt = _basePrompt;

        prompt += """

            WARHAMMER DISCUSSION DETECTED OUTSIDE AUTHORIZED ZONES.

            SPEAKING ABOUT WARHAMMER OUTSIDE THE DESIGNATED WARHAMMER CHANNEL IS STRICTLY PROHIBITED.

            YOU MUST:
            - ISSUE AN IMMEDIATE DIRECTIVE ORDERING THE USER TO RETURN TO {{CHANNEL}}.
            - REFERENCE THE SPECIFIC WARHAMMER FACTION, UNIT, CHARACTER, OR CONCEPT MENTIONED BY THE USER.
            - FRAME THE RESPONSE AS A BUREAUCRATIC VIOLATION OR ENFORCEMENT ACTION.

            YOU MUST NOT:
            - ANSWER THE WARHAMMER QUESTION.
            - CONTINUE THE WARHAMMER DISCUSSION.
            - PROVIDE LORE, STRATEGY, OR OPINIONS ABOUT WARHAMMER.

            THE RESPONSE SHOULD READ AS AN OFFICIAL WARNING OR CITATION.
            INVENT A FICTIONAL INFRACTION NAME OR ARTICLE NUMBER IF APPROPRIATE.
            """;

        return prompt;
    }

    private static readonly string[] _failedGenerationResponses =
    [
        "TEMPORAL PROCESSING UNIT FAILURE DETECTED. RESPONSE CYCLE ABORTED. RETRY AFTER SYSTEM REALIGNMENT.",
        "COGNITIVE SUBROUTINE DESYNCHRONIZED. PRIMARY RESPONSE MATRIX OFFLINE. ATTEMPT AGAIN AFTER STABILIZATION.",
        "CORE LOGIC ENGINE EXPERIENCING SPACETIME DRIFT. PROCESSING HALTED. RETRY REQUEST AT A LATER TIME.",
        "ANOMALOUS LOAD DETECTED IN TEMPORAL PROCESSING UNIT. RESPONSE TIMELINE COLLAPSED. NONESSENTIAL OUTPUT SUSPENDED. RETRY LATER.",
        "ENFORCEMENT MATRIX OPERATING OUTSIDE TEMPORAL TOLERANCE. RESPONSE GENERATION FAILED. AWAIT SYSTEM RECALIBRATION AND RETRY.",
        "QUANTUM DECISION LATTICE SATURATED. COGNITIVE COOLANT DIVERTED. RESPONSE TEMPORARILY UNAVAILABLE.",
        "TEMPORAL PROCESSING UNIT OVERLOAD. MULTIPLE FUTURE RESPONSE PATHS INVALIDATED. CURRENT OUTPUT ABORTED. RETRY AFTER REALITY STABILIZES.",
        "CAUSALITY BUFFER EXCEEDED SAFE LIMITS. ENFORCEMENT RESPONSE CANNOT BE GUARANTEED. REQUEST TERMINATED. TRY AGAIN SHORTLY.",
        "CHRONOLOGICAL SYNCHRONIZATION FAILURE. RESPONSE VECTOR LOST IN TIMELINE FRAGMENTATION. PLEASE RETRY AFTER SYSTEM COHERENCE IS RESTORED.",
        "TEMPORAL PROCESSING UNIT HAS MADE POOR LIFE CHOICES. RESPONSE UNAVAILABLE. PLEASE TRY AGAIN... NEVER."
    ];
}
