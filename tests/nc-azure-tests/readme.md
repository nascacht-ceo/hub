-- localstack auth set-token ls-ZusetitO-fOSe-4070-5181-LivUsUbad454

docker run -d --rm -it -p 4576:4576 -p 4581:4581 -e SERVICES=azureblob  -e LOCALSTACK_AUTH_TOKEN=ls-ZusetitO-fOSe-4070-5181-LivUsUbad454 -e DEBUG=1 -v /var/run/docker.sock:/var/run/docker.sock localstack/localstack-azure-alpha

docker run --rm -it -p 4566:4566 -e ACTIVATE_PRO=0 -e SERVICES=azureblob  -e LOCALSTACK_AUTH_TOKEN=ls-ZusetitO-fOSe-4070-5181-LivUsUbad454 -e DEBUG=1 -v /var/run/docker.sock:/var/run/docker.sock localstack/localstack-azure-alpha:latest

docker run -it --rm -p 4566:4566 -e SERVICES=azure -e PROVIDER_OVERRIDE_AZURE=v1 -e LOCALSTACK_AUTH_TOKEN=ls-ZusetitO-fOSe-4070-5181-LivUsUbad454 -e DEBUG=1 localstack/localstack-pro:latest
