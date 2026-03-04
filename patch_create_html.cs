<<<<<<< SEARCH
                                const input = document.createElement('input');
                                input.className = 'form-check-input';
                                input.type = 'checkbox';
                                input.name = 'SuggestedEntityIds';
                                input.value = sugg.existingContactId;
                                input.id = `sugg_${sugg.existingContactId}`;

                                const label = document.createElement('label');
                                label.className = 'form-check-label fw-bold';
                                label.htmlFor = input.id;
                                label.textContent = `Is ${sugg.sourceName} also a ${sugg.relationshipName} of ${sugg.targetName}?`;
=======
                                const input = document.createElement('input');
                                input.className = 'form-check-input';
                                input.type = 'checkbox';
                                input.name = 'SuggestedRelationships';
                                input.value = sugg.payload;
                                input.id = `sugg_${sugg.payload}`;

                                const label = document.createElement('label');
                                label.className = 'form-check-label fw-bold';
                                label.htmlFor = input.id;
                                label.textContent = `Is ${sugg.sourceName} also a ${sugg.relationshipName} of ${sugg.targetName}?`;
>>>>>>> REPLACE
