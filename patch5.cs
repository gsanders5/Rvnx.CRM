<<<<<<< SEARCH
                    this.preview.appendChild(s2);

                    this.preview.appendChild(document.createTextNode('.'));
                }
            }

            const entityName = @Html.Raw(Json.Serialize(Model.EntityName));
=======
                    this.preview.appendChild(s2);

                    this.preview.appendChild(document.createTextNode('.'));

                    this.fetchSuggestions(relatedName);
                }

                async fetchSuggestions(relatedName) {
                    if (!this.suggestionsContainer) return;

                    const entityId = '@Model.EntityId';
                    let url = `/Relationships/GetSuggestions?entityId=${entityId}&relationshipType=${this.hiddenInput.value}`;

                    if (this.isPartialForm) {
                        url += `&partialContactName=${encodeURIComponent(relatedName)}`;
                    } else {
                        const relatedId = this.relatedInput.value;
                        if (!relatedId) {
                            this.suggestionsContainer.style.display = 'none';
                            return;
                        }
                        url += `&relatedEntityId=${relatedId}`;
                    }

                    try {
                        const response = await fetch(url);
                        if (!response.ok) throw new Error('Network response was not ok');
                        const data = await response.json();

                        this.suggestionsContainer.innerHTML = '';

                        if (data && data.length > 0) {
                            const p = document.createElement('p');
                            p.className = 'fw-bold mb-2';
                            p.textContent = 'Also apply this relationship to:';
                            this.suggestionsContainer.appendChild(p);

                            data.forEach(sugg => {
                                const div = document.createElement('div');
                                div.className = 'form-check mb-1';

                                const input = document.createElement('input');
                                input.className = 'form-check-input';
                                input.type = 'checkbox';
                                input.name = 'SuggestedEntityIds';
                                input.value = sugg.existingContactId;
                                input.id = `sugg_${sugg.existingContactId}`;

                                const label = document.createElement('label');
                                label.className = 'form-check-label';
                                label.htmlFor = input.id;
                                label.textContent = `Is ${sugg.sourceName} also a ${sugg.relationshipName} of ${sugg.targetName}?`;

                                div.appendChild(input);
                                div.appendChild(label);
                                this.suggestionsContainer.appendChild(div);
                            });

                            this.suggestionsContainer.style.display = 'block';
                        } else {
                            this.suggestionsContainer.style.display = 'none';
                        }
                    } catch (error) {
                        console.error('Error fetching suggestions:', error);
                        this.suggestionsContainer.style.display = 'none';
                    }
                }
            }

            const entityName = @Html.Raw(Json.Serialize(Model.EntityName));
>>>>>>> REPLACE
