# Runs migrations on all DB contexts, which populates the DB with their corresponding models.

project=${0%/*}/../LeaderboardBackend
contexts=`ll $project/Models | rg -xN '(\w+?Context)\.cs' -r '$1'`

echo $contexts | while read context; do
	echo "Running migration for $context..."
	dotnet ef -p $project database update -c $context
done

unset contexts
unset project
